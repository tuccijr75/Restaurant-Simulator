using Godot;
using System.Collections.Generic;

namespace RestaurantSimulator;

/// Drive-thru / delivery vehicle: procedural body + cabin + wheels, follows the
/// lane waypoints, pauses at the order board, holds at the window until its
/// ticket completes, then exits.
public partial class CarAgent : Node3D
{
    public string OrderId = "";
    public bool TicketDone;
    public int LaneIndex;                  // current waypoint target
    public CarAgent? Ahead;                // simple car-following spacing
    public List<Vector3> Lane = new();
    float _boardPause = 8f;                // visual ordering pause (real seconds)
    public bool Exiting;
    const float Speed = 4.0f;
    StandardMaterial3D _brakeMat = null!;   // RS-VS-001: glows while held in queue

    public void BuildCar(Color paint, int variant = 0)
    {
        // RS-VS-001 body variants: 0 sedan, 1 SUV (taller cabin), 2 pickup (open bed)
        if (variant == 1)
        {
            Add(new BoxMesh { Size = new Vector3(1.75f, 0.7f, 3.3f) }, new Vector3(0, 0.62f, 0), paint);
            Add(new BoxMesh { Size = new Vector3(1.6f, 0.62f, 2.1f) }, new Vector3(0, 1.22f, -0.2f), paint.Darkened(0.2f));
        }
        else if (variant == 2)
        {
            Add(new BoxMesh { Size = new Vector3(1.7f, 0.6f, 1.9f) }, new Vector3(0, 0.58f, 0.7f), paint);
            Add(new BoxMesh { Size = new Vector3(1.7f, 0.35f, 1.5f) }, new Vector3(0, 0.46f, -0.95f), paint.Darkened(0.35f));
            Add(new BoxMesh { Size = new Vector3(1.5f, 0.5f, 1.0f) }, new Vector3(0, 1.05f, 0.75f), paint.Darkened(0.2f));
        }
        else
        {
            Add(new BoxMesh { Size = new Vector3(1.7f, 0.55f, 3.4f) }, new Vector3(0, 0.55f, 0), paint);
            Add(new BoxMesh { Size = new Vector3(1.5f, 0.5f, 1.7f) }, new Vector3(0, 1.05f, -0.1f), paint.Darkened(0.25f));
        }
        _brakeMat = new StandardMaterial3D { AlbedoColor = new Color(0.5f, 0.05f, 0.05f), EmissionEnabled = true, Emission = new Color(1f, 0.1f, 0.05f), EmissionEnergyMultiplier = 0f };
        foreach (var x in new[] { -0.62f, 0.62f })
            AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.3f, 0.14f, 0.06f) }, Position = new Vector3(x, 0.62f, -1.72f), MaterialOverride = _brakeMat });
        var glass = Add(new BoxMesh { Size = new Vector3(1.52f, 0.34f, 1.0f) }, new Vector3(0, 1.1f, 0.55f), new Color(0.4f, 0.6f, 0.7f, 0.6f));
        ((StandardMaterial3D)glass.MaterialOverride).Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        foreach (var (x, z) in new[] { (-0.8f, 1.1f), (0.8f, 1.1f), (-0.8f, -1.1f), (0.8f, -1.1f) })
        {
            var wmesh = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.32f, BottomRadius = 0.32f, Height = 0.25f },
                Position = new Vector3(x, 0.32f, z),
                RotationDegrees = new Vector3(0, 0, 90),
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.08f, 0.08f, 0.08f) }
            };
            AddChild(wmesh);
        }
    }

    MeshInstance3D Add(Mesh mesh, Vector3 pos, Color c)
    {
        var mi = new MeshInstance3D
        {
            Mesh = mesh, Position = pos,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = c, Roughness = 0.4f, Metallic = 0.4f }
        };
        AddChild(mi);
        return mi;
    }

    // Forward axis of the supplied car/truck GLBs is unknown until seen in-engine.
    // If cars drive backwards, flip this to 180. The brake-light code is skipped
    // for GLB cars (no procedural _brakeMat), so queued cars just hold position.
    const float CarModelYawDeg = 0f;

    /// Load a real vehicle GLB (res://models/vehicles/car.glb|truck.glb). Returns
    /// false if missing so the caller can fall back to the procedural box car.
    public bool BuildCarModel(string resPath)
    {
        if (!ResourceLoader.Exists(resPath)) return false;
        var packed = ResourceLoader.Load<PackedScene>(resPath);
        if (packed == null) return false;
        var inst = packed.InstantiateOrNull<Node3D>();
        if (inst == null) return false;
        inst.RotationDegrees = new Vector3(0, CarModelYawDeg, 0);
        AddChild(inst);
        // sit the body base on the ground (model origin may be centered)
        var aabb = ModelAabb(inst);
        if (aabb.Size != Vector3.Zero) inst.Position = new Vector3(0, -aabb.Position.Y, 0);
        return true;
    }

    static Aabb ModelAabb(Node3D root)
    {
        Aabb box = default; bool has = false;
        void Walk(Node node, Transform3D parentToRoot)
        {
            var toRoot = parentToRoot;
            if (node is Node3D n3 && node != root)
                toRoot *= n3.Transform;

            if (node is MeshInstance3D mi && mi.Mesh != null)
            {
                // BuildCarModel can run before this CarAgent enters the scene tree.
                // Stay in local transforms so model bounds never depend on GlobalTransform.
                var world = toRoot * mi.GetAabb();
                if (!has) { box = world; has = true; } else box = box.Merge(world);
            }
            foreach (var c in node.GetChildren()) Walk(c, toRoot);
        }
        Walk(root, Transform3D.Identity);
        return box;
    }

    /// Returns true when the car has left the lot and can be freed.
    public bool Drive(float delta, int boardIdx, int windowIdx)
    {
        if (LaneIndex >= Lane.Count) return true;
        var target = Lane[LaneIndex];
        if (_brakeMat != null) _brakeMat.EmissionEnergyMultiplier = 0f;   // cleared unless a hold-path below lights it

        // Keep distance behind the car ahead.
        if (Ahead != null && IsInstanceValid(Ahead) && !Ahead.Exiting)
        {
            var gap = (Ahead.Position - Position).Length();
            if (gap < 4.2f && LaneIndex <= Ahead.LaneIndex) { if (_brakeMat != null) _brakeMat.EmissionEnergyMultiplier = 2.2f; return false; }
        }

        var pos = Position; pos.Y = 0;
        var d = target - pos;
        if (d.Length() < 0.15f)
        {
            if (LaneIndex == boardIdx && _boardPause > 0) { _boardPause -= delta; { if (_brakeMat != null) _brakeMat.EmissionEnergyMultiplier = 2.2f; return false; } }
            if (LaneIndex == windowIdx && !TicketDone) { if (_brakeMat != null) _brakeMat.EmissionEnergyMultiplier = 2.2f; return false; }
            LaneIndex++;
            if (LaneIndex >= Lane.Count) { Exiting = true; return true; }
            return false;   // advancing to next waypoint
        }
        var step = d.Normalized() * Mathf.Min(Speed * delta, d.Length());
        Position += step;
        Rotation = new Vector3(0, Mathf.Atan2(step.X, step.Z), 0);
        return false;   // rolling — brakes stay dark
    }
}
