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
    Camera3D _follow = null!;
    Node3D? _followTarget;
    string _followName = "";
    int _current;
    bool _tour, _freeMode, _followMode;
    float _tourTimer;
    const float TourSeconds = 7f;
    Vector2 _look;

    public string CurrentName => _followMode ? "FOLLOW " + _followName : _freeMode ? "FREE CAM" : _cams.Count == 0 ? "-" : _cams[_current].Name;
    public bool TourOn => _tour && !_followMode;
    public bool IsOverhead => !_freeMode && !_followMode && _cams.Count > 0 && _cams[_current].Name.Contains("OVERHEAD");
    public bool IsFreeHigh => _freeMode && _free.Position.Y > 5.2f;

    public void Build(Node3D root, WorldLayout w)
    {
        // (name, mount position, look-at target)
        var defs = new (string, Vector3, Vector3)[]
        {
            ("CAM-01 GRILL",        new Vector3(5.8f, 3.55f, -2.0f),  w.Anchor["grill"] + Vector3.Up * 0.75f),
            ("CAM-02 FRYER",        new Vector3(-3.8f, 3.55f, -2.0f), w.Anchor["fryer"] + Vector3.Up * 0.75f),
            ("CAM-03 PREP/WALK-IN", new Vector3(6.5f, 3.55f, -2.2f),  (w.Anchor["prep"] + w.Anchor["cooler"]) / 2 + Vector3.Up * 0.75f),
            ("CAM-04 ASSEMBLY",     new Vector3(-2.4f, 3.35f, -1.35f), w.Anchor["assembly"] + Vector3.Up * 0.7f),
            ("CAM-05 EXPO/PICKUP",  new Vector3(-2.3f, 3.15f, 0.75f), (w.Anchor["expo"] + w.Anchor["mobile_shelf"]) / 2 + Vector3.Up * 0.75f),
            ("CAM-06 FRONT COUNTER",new Vector3(5.4f, 3.0f, 4.8f),    new Vector3(-0.2f, 1.0f, -0.8f)),
            ("CAM-07 LOBBY/DINING", new Vector3(10.6f, 3.2f, 5.7f),   new Vector3(-4f, 0.75f, 3.2f)),
            ("CAM-08 DT WINDOW",    new Vector3(-9.5f, 3.05f, -0.8f), w.Anchor["dt_window"] + new Vector3(0.4f, 0.65f, -0.1f)),
            ("CAM-09 DT LANE/BOARD",new Vector3(-24.0f, 6.2f, 7.5f),  w.Anchor["order_board"] + new Vector3(1.8f, 0.05f, 0.8f)),
            ("CAM-10 LOT/ENTRANCE", new Vector3(-13.8f, 4.7f, 16.2f), new Vector3(0, 0.8f, 8.3f)),
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

        _follow = new Camera3D { Position = new Vector3(0, 2.4f, 10), Fov = 62 };
        root.AddChild(_follow);

        Select(5); // start on front counter
    }

    public override void _ExitTree()
    {
        _followTarget = null;
        _followMode = false;
        _freeMode = false;
        _tour = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }

    public void Select(int i)
    {
        if (i < 0 || i >= _cams.Count) return;
        _current = i;
        _freeMode = false;
        _followMode = false;
        _followTarget = null;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _cams[_current].Cam.MakeCurrent();
    }

    public void NextCam() { _tour = false; Select((_current + 1) % _cams.Count); }
    public void Overhead() { _tour = false; Select(_cams.Count - 1); }
    public void ToggleTour() { _tour = !_tour; _tourTimer = 0; if (_tour && _freeMode) Select(_current); }

    public void Follow(Node3D target, string label)
    {
        if (target == null || !IsInstanceValid(target)) return;
        _tour = false;
        _freeMode = false;
        _followMode = true;
        _followTarget = target;
        _followName = label;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        UpdateFollowCamera(1f);
        _follow.MakeCurrent();
    }

    public void ToggleFree()
    {
        _freeMode = !_freeMode;
        _tour = false;
        _followMode = false;
        _followTarget = null;
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
        if (_followMode)
        {
            if (_followTarget == null || !IsInstanceValid(_followTarget)) { Select(_current); return; }
            UpdateFollowCamera((float)delta);
        }
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

    void UpdateFollowCamera(float delta)
    {
        if (_followTarget == null) return;
        var target = _followTarget.GlobalPosition + Vector3.Up * 1.15f;
        var back = -_followTarget.GlobalTransform.Basis.Z;
        back.Y = 0f;
        if (back.LengthSquared() < 0.01f) back = Vector3.Back;
        back = back.Normalized();
        var desired = target + back * 3.8f + Vector3.Up * 1.55f;
        float t = Mathf.Clamp(1f - Mathf.Exp(-7f * delta), 0f, 1f);
        _follow.GlobalPosition = _follow.GlobalPosition.Lerp(desired, t);
        if (_follow.GlobalPosition.DistanceSquaredTo(target) > 0.04f)
            _follow.LookAt(target, Vector3.Up);
    }
}
