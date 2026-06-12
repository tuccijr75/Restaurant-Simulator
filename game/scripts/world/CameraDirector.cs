using Godot;
using System.Collections.Generic;

namespace RestaurantSimulator;

/// CCTV-style camera system. Every operational station has a dedicated mounted
/// camera (visible mount box on the wall), plus exterior lane/lot coverage, an
/// overhead floorplan view, and a WASD+mouse free camera.
/// Keys: 1-9,0 select cams 1-10 · O overhead · C next · T auto-tour · F free cam.
public partial class CameraDirector : Node3D
{
    public sealed class CamDef
    {
        public string Name = "";
        public Camera3D Cam = null!;
    }

    readonly List<CamDef> _cams = new();
    Camera3D _free = null!;
    int _current;
    bool _tour, _freeMode;
    float _tourTimer;
    const float TourSeconds = 7f;
    Vector2 _look;

    public string CurrentName => _freeMode ? "FREE CAM" : _cams.Count == 0 ? "-" : _cams[_current].Name;
    public bool TourOn => _tour;
    public bool IsOverhead => !_freeMode && _cams.Count > 0 && _cams[_current].Name.Contains("OVERHEAD");
    public bool IsFreeHigh => _freeMode && _free.Position.Y > 5.2f;

    public void Build(Node3D root, WorldLayout w)
    {
        // (name, mount position, look-at target)
        var defs = new (string, Vector3, Vector3)[]
        {
            ("CAM-01 GRILL",        new Vector3(-8.5f, 3.2f, -6.5f), w.Anchor["grill"] + Vector3.Up * 0.6f),
            ("CAM-02 FRYER",        new Vector3(-4.2f, 3.2f, -6.5f), w.Anchor["fryer"] + Vector3.Up * 0.6f),
            ("CAM-03 PREP/WALK-IN", new Vector3(-1.8f, 3.2f, -6.5f), (w.Anchor["prep"] + w.Anchor["cooler"]) / 2 + Vector3.Up * 0.6f),
            ("CAM-04 ASSEMBLY",     new Vector3(-3.0f, 3.2f, 0.6f),  w.Anchor["assembly"] + Vector3.Up * 0.5f),
            ("CAM-05 BEV/EXPO",     new Vector3(3.6f, 3.2f, 0.9f),   (w.Anchor["beverage"] + w.Anchor["expo"]) / 2 + Vector3.Up * 0.6f),
            ("CAM-06 FRONT COUNTER",new Vector3(-0.5f, 2.3f, 5.4f),  new Vector3(-0.5f, 0.95f, -0.8f)),
            ("CAM-07 LOBBY/DINING", new Vector3(11.2f, 3.1f, 6.3f),  new Vector3(-3f, 0.7f, 1.0f)),
            ("CAM-08 DT WINDOW",    new Vector3(10.9f, 3.2f, -2.6f), w.Anchor["dt_window"] + new Vector3(2.0f, 0.4f, 0)),
            ("CAM-09 DT LANE/BOARD",new Vector3(18.5f, 3.6f, -10f),  w.Anchor["order_board"] + new Vector3(-1.5f, -0.4f, 2f)),
            ("CAM-10 LOT/ENTRANCE", new Vector3(-15f, 4.6f, 13f),    new Vector3(0, 0.8f, 7.5f)),
        };
        foreach (var (name, pos, target) in defs)
        {
            WorldBuilder.Box(root, new Vector3(0.28f, 0.2f, 0.42f), pos, new Color(0.12f, 0.12f, 0.14f), "mount_" + name);
            var cam = new Camera3D { Position = pos, Fov = 65 };
            root.AddChild(cam);
            cam.LookAt(target, Vector3.Up);
            _cams.Add(new CamDef { Name = name, Cam = cam });
        }
        // Overhead floorplan (index 10, key O)
        var over = new Camera3D { Position = new Vector3(0, 30, 4), RotationDegrees = new Vector3(-90, 0, 0), Fov = 55 };
        root.AddChild(over);
        _cams.Add(new CamDef { Name = "CAM-11 OVERHEAD", Cam = over });

        _free = new Camera3D { Position = new Vector3(0, 2.2f, 14), Fov = 70 };
        root.AddChild(_free);
        _free.LookAt(new Vector3(0, 1.2f, 0), Vector3.Up);
        _look = new Vector2(_free.Rotation.Y, _free.Rotation.X);

        Select(5); // start on front counter
    }

    public void Select(int i)
    {
        if (i < 0 || i >= _cams.Count) return;
        _current = i;
        _freeMode = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _cams[_current].Cam.MakeCurrent();
    }

    public void NextCam() { _tour = false; Select((_current + 1) % _cams.Count); }
    public void Overhead() { _tour = false; Select(_cams.Count - 1); }
    public void ToggleTour() { _tour = !_tour; _tourTimer = 0; if (_tour && _freeMode) Select(_current); }

    public void ToggleFree()
    {
        _freeMode = !_freeMode;
        _tour = false;
        if (_freeMode)
        {
            _free.MakeCurrent();
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _cams[_current].Cam.MakeCurrent();
        }
    }

    public void FreeLook(Vector2 relative)
    {
        if (!_freeMode) return;
        _look.X -= relative.X * 0.0035f;
        _look.Y = Mathf.Clamp(_look.Y - relative.Y * 0.0035f, -1.4f, 1.4f);
        _free.Rotation = new Vector3(_look.Y, _look.X, 0);
    }

    public override void _Process(double delta)
    {
        if (_tour)
        {
            _tourTimer += (float)delta;
            if (_tourTimer >= TourSeconds)
            {
                _tourTimer = 0;
                Select((_current + 1) % _cams.Count);
                _tour = true;
            }
        }
        if (_freeMode)
        {
            var dir = Vector3.Zero;
            if (Input.IsKeyPressed(Key.W)) dir -= _free.Basis.Z;
            if (Input.IsKeyPressed(Key.S)) dir += _free.Basis.Z;
            if (Input.IsKeyPressed(Key.A)) dir -= _free.Basis.X;
            if (Input.IsKeyPressed(Key.D)) dir += _free.Basis.X;
            if (Input.IsKeyPressed(Key.Q)) dir += Vector3.Up;
            if (Input.IsKeyPressed(Key.Z)) dir -= Vector3.Up;
            float speed = Input.IsKeyPressed(Key.Shift) ? 14f : 6f;
            if (dir.LengthSquared() > 0.001f)
                _free.Position += dir.Normalized() * speed * (float)delta;
        }
    }
}
