using Godot;

namespace RestaurantSimulator;

/// Procedural low-poly humanoid with code-driven animation (walk bob + arm swing,
/// working motion, idle breathing). No imported rigs, so every character ships
/// with the repo and animates deterministically from agent state.
public partial class CharacterRig : Node3D
{
    MeshInstance3D _body = null!, _head = null!, _armL = null!, _armR = null!, _legL = null!, _legR = null!, _hat = null!;
    float _t;
    public bool Moving, Working;
    public float WalkSpeed = 1.5f;

    public void BuildHuman(Color shirt, Color pants, Color skin, Color? hat = null, float heightScale = 1f)
    {
        Scale = new Vector3(Mathf.Clamp(heightScale, 0.9f, 1.1f), Mathf.Clamp(heightScale, 0.9f, 1.1f), Mathf.Clamp(heightScale, 0.9f, 1.1f));
        _legL = Limb(new Vector3(-0.11f, 0.38f, 0), 0.075f, 0.62f, pants);
        _legR = Limb(new Vector3(0.11f, 0.38f, 0), 0.075f, 0.62f, pants);
        _body = Limb(new Vector3(0, 1.02f, 0), 0.21f, 0.62f, shirt);
        _head = Sphere(new Vector3(0, 1.55f, 0), 0.155f, skin);
        _armL = Limb(new Vector3(-0.30f, 1.12f, 0), 0.06f, 0.52f, shirt);
        _armR = Limb(new Vector3(0.30f, 1.12f, 0), 0.06f, 0.52f, shirt);
        if (hat.HasValue)
        {
            _hat = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.17f, BottomRadius = 0.17f, Height = 0.10f },
                Position = new Vector3(0, 1.73f, 0),
                MaterialOverride = Mat(hat.Value)
            };
            AddChild(_hat);
        }
    }

    MeshInstance3D Limb(Vector3 pos, float r, float h, Color c)
    {
        var mi = new MeshInstance3D
        {
            Mesh = new CapsuleMesh { Radius = r, Height = h },
            Position = pos,
            MaterialOverride = Mat(c)
        };
        AddChild(mi);
        return mi;
    }

    MeshInstance3D Sphere(Vector3 pos, float r, Color c)
    {
        var mi = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = r, Height = r * 2 },
            Position = pos,
            MaterialOverride = Mat(c)
        };
        AddChild(mi);
        return mi;
    }

    static StandardMaterial3D Mat(Color c) => new() { AlbedoColor = c, Roughness = 0.9f };

    public override void _Process(double delta)
    {
        _t += (float)delta;
        if (Moving)
        {
            float swing = Mathf.Sin(_t * 7.5f) * 0.55f;
            _armL.Rotation = new Vector3(swing, 0, 0);
            _armR.Rotation = new Vector3(-swing, 0, 0);
            _legL.Rotation = new Vector3(-swing * 0.8f, 0, 0);
            _legR.Rotation = new Vector3(swing * 0.8f, 0, 0);
            _body.Position = new Vector3(0, 1.02f + Mathf.Abs(Mathf.Sin(_t * 7.5f)) * 0.04f, 0);
        }
        else if (Working)
        {
            float chop = Mathf.Sin(_t * 9f) * 0.45f;
            _armL.Rotation = new Vector3(-0.9f + chop * 0.4f, 0, 0.15f);
            _armR.Rotation = new Vector3(-0.9f - chop * 0.4f, 0, -0.15f);
            _legL.Rotation = Vector3.Zero;
            _legR.Rotation = Vector3.Zero;
            _body.Position = new Vector3(0, 1.02f, 0.02f * Mathf.Sin(_t * 9f));
        }
        else
        {
            float breathe = Mathf.Sin(_t * 1.6f) * 0.012f;
            _armL.Rotation = new Vector3(0.05f, 0, 0.08f);
            _armR.Rotation = new Vector3(0.05f, 0, -0.08f);
            _legL.Rotation = Vector3.Zero;
            _legR.Rotation = Vector3.Zero;
            _body.Position = new Vector3(0, 1.02f + breathe, 0);
        }
    }

    /// Move toward a flat target; returns true on arrival. Handles facing.
    public bool StepToward(Vector3 target, float delta)
    {
        var pos = Position; pos.Y = 0; target.Y = 0;
        var d = target - pos;
        float dist = d.Length();
        if (dist < 0.08f) { Moving = false; return true; }
        Moving = true;
        var step = d.Normalized() * Mathf.Min(WalkSpeed * delta, dist);
        Position += step;
        float yaw = Mathf.Atan2(step.X, step.Z);
        Rotation = new Vector3(0, yaw, 0);
        return false;
    }
}
