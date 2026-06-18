using Godot;
using System.Collections.Generic;

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
    public bool DestinationBlocked { get; private set; }
    public float DestinationBlockedSeconds { get; private set; }
    static readonly List<CharacterRig> ActiveRigs = new();

    // RS-VS-002: optional imported GLB body (staff). When present the procedural
    // limbs aren't built; the model's own AnimationPlayer drives walk/idle/work.
    Node3D _modelInstance = null!;
    bool _usesModel;
    float _modelBaseY;
    AnimationPlayer? _anim;
    readonly List<string> _clips = new();
    string _curClip = "";
    /// Station-specific work-animation keyword, set by EmployeeAgent (e.g. "grill").
    public string WorkAnimKey = "";
    /// Persistent state action set by the agent (e.g. "sitting", "sweeping"); wins
    /// over work/idle while standing. Empty = normal walk/work/idle selection.
    public string ActionAnim = "";
    string _oneShotAnim = "";
    float _oneShotTimer;
    // (13) sweep stroke: scrub only part of the sweep clip, back and forth, so the broom
    // never swings all the way past the body (no "golf swing"). Tune in-editor.
    public static float SweepRate = 4.5f;     // strokes per second (speed of the back-and-forth)
    public static float SweepStroke = 0.42f;  // fraction of the sweep clip used (0..1); lower = shorter stroke

    // Procedural fallback: if the model is rigged but ships no clips, drive its
    // skeleton bones directly (snippet-2 approach) so it still walks/works.
    Skeleton3D? _skel;
    int _bSpine = -1, _bArmL = -1, _bArmR = -1, _bLegL = -1, _bLegR = -1;
    bool _boneDriven;

    public override void _EnterTree()
    {
        if (!ActiveRigs.Contains(this)) ActiveRigs.Add(this);
    }

    public override void _ExitTree()
    {
        ActiveRigs.Remove(this);
    }

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

    /// RS-VS-002: swap the procedural body for an imported model (res://...glb).
    /// Returns false if the resource is missing or isn't a Node3D, so the caller
    /// can fall back to BuildHuman and never end up with an invisible agent.
    public bool BuildModel(string resPath, float scale = 1f, float yawDegrees = 0f, float yOffset = 0f)
    {
        var packed = ResourceLoader.Load<PackedScene>(resPath);
        if (packed == null) return false;
        var node = packed.Instantiate();
        if (node is not Node3D inst) { node.QueueFree(); return false; }
        _modelBaseY = yOffset;
        inst.Scale = new Vector3(scale, scale, scale);
        inst.RotationDegrees = new Vector3(0, yawDegrees, 0);
        inst.Position = new Vector3(0, yOffset, 0);
        AddChild(inst);
        _modelInstance = inst;
        _usesModel = true;

        // Discover the model's animation clips and make them loop so a held state
        // (walking, working, idle) plays continuously rather than freezing.
        _anim = FindAnim(inst);
        if (_anim != null)
        {
            foreach (var n in _anim.GetAnimationList())
            {
                _clips.Add(n);
                var clip = _anim.GetAnimation(n);
                if (clip != null)
                {
                    var lc = n.ToLowerInvariant();
                    // sweep = single stroke -> ping-pong; sit/stand = one-shot transitions
                    // that hold their last pose; everything else loops.
                    clip.LoopMode = lc.Contains("sweep") ? Animation.LoopModeEnum.Pingpong
                                  : (lc == "sit" || lc == "stand") ? Animation.LoopModeEnum.None
                                  : Animation.LoopModeEnum.Linear;
                }
            }
        }
        GD.Print($"[Agent] {System.IO.Path.GetFileName(resPath)} anims: " +
                 (_clips.Count > 0 ? string.Join(", ", _clips) : "<none — model has no AnimationPlayer/clips>"));

        // No baked clips? Fall back to driving the rig's bones directly.
        if (_clips.Count == 0)
        {
            _skel = FindSkel(inst);
            if (_skel != null)
            {
                string[] L = { "left", ".l", "_l", "lft", "lupper", "l_" };
                string[] R = { "right", ".r", "_r", "rgt", "rupper", "r_" };
                _bSpine = FindBone(new[] { "spine", "chest", "torso" }, null, null);
                _bArmL = FindBone(new[] { "arm", "shoulder", "upperarm" }, L, new[] { "fore", "hand", "lower" });
                _bArmR = FindBone(new[] { "arm", "shoulder", "upperarm" }, R, new[] { "fore", "hand", "lower" });
                _bLegL = FindBone(new[] { "upleg", "thigh", "upperleg", "upper_leg", "hip", "leg" }, L, new[] { "lower", "fore", "foot", "toe" });
                _bLegR = FindBone(new[] { "upleg", "thigh", "upperleg", "upper_leg", "hip", "leg" }, R, new[] { "lower", "fore", "foot", "toe" });
                _boneDriven = _bArmL >= 0 || _bArmR >= 0 || _bLegL >= 0 || _bLegR >= 0;
                GD.Print($"[Agent] {System.IO.Path.GetFileName(resPath)} skeleton bones={_skel.GetBoneCount()} " +
                         $"armL={BoneName(_bArmL)} armR={BoneName(_bArmR)} legL={BoneName(_bLegL)} legR={BoneName(_bLegR)} spine={BoneName(_bSpine)} " +
                         $"=> {(_boneDriven ? "procedural bone-drive ON" : "no recognizable limb bones")}");
            }
        }
        return true;
    }

    string BoneName(int idx) => _skel != null && idx >= 0 ? _skel.GetBoneName(idx) : "-";

    static Skeleton3D? FindSkel(Node n)
    {
        if (n is Skeleton3D sk) return sk;
        foreach (var c in n.GetChildren())
        {
            var r = FindSkel(c);
            if (r != null) return r;
        }
        return null;
    }

    // First bone whose lowercased name contains any 'parts' keyword and (if given)
    // a 'side' marker, while containing none of the 'exclude' keywords.
    int FindBone(string[] parts, string[]? side, string[]? exclude)
    {
        if (_skel == null) return -1;
        for (int i = 0; i < _skel.GetBoneCount(); i++)
        {
            var lc = _skel.GetBoneName(i).ToLowerInvariant();
            bool hasPart = false; foreach (var p in parts) if (lc.Contains(p)) { hasPart = true; break; }
            if (!hasPart) continue;
            if (exclude != null) { bool ex = false; foreach (var e in exclude) if (lc.Contains(e)) { ex = true; break; } if (ex) continue; }
            if (side != null) { bool sd = false; foreach (var s in side) if (lc.Contains(s)) { sd = true; break; } if (!sd) continue; }
            return i;
        }
        return -1;
    }

    void Swing(int idx, Vector3 axis, float angle)
    {
        if (idx < 0 || _skel == null) return;
        var rest = _skel.GetBoneRest(idx).Basis.GetRotationQuaternion();
        _skel.SetBonePoseRotation(idx, rest * new Quaternion(axis.Normalized(), angle));
    }

    void DriveBones()
    {
        // Code-authored gait/work cycles applied on top of each bone's rest pose.
        if (Moving)
        {
            float s = Mathf.Sin(_t * 7.5f) * 0.5f;
            Swing(_bArmL, Vector3.Right, s);
            Swing(_bArmR, Vector3.Right, -s);
            Swing(_bLegL, Vector3.Right, -s * 0.9f);
            Swing(_bLegR, Vector3.Right, s * 0.9f);
            Swing(_bSpine, Vector3.Up, Mathf.Sin(_t * 7.5f) * 0.04f);
        }
        else if (Working)
        {
            float chop = Mathf.Sin(_t * 9f) * 0.45f;
            Swing(_bArmL, Vector3.Right, -0.9f + chop * 0.4f);
            Swing(_bArmR, Vector3.Right, -0.9f - chop * 0.4f);
            Swing(_bLegL, Vector3.Right, 0f);
            Swing(_bLegR, Vector3.Right, 0f);
        }
        else
        {
            float br = Mathf.Sin(_t * 1.6f) * 0.05f;
            Swing(_bArmL, Vector3.Forward, 0.06f + br * 0.2f);
            Swing(_bArmR, Vector3.Forward, -0.06f - br * 0.2f);
            Swing(_bSpine, Vector3.Up, br);
        }
    }

    static AnimationPlayer? FindAnim(Node n)
    {
        if (n is AnimationPlayer ap) return ap;
        foreach (var c in n.GetChildren())
        {
            var r = FindAnim(c);
            if (r != null) return r;
        }
        return null;
    }

    // Pick a clip whose name contains any of the given keywords (case-insensitive).
    string? Match(params string[] keys)
    {
        foreach (var c in _clips)
        {
            var lc = c.ToLowerInvariant();
            foreach (var k in keys)
                if (k.Length > 0 && lc.Contains(k)) return c;
        }
        return null;
    }

    string PickIdle() => Match("idle", "breath", "rest", "wait") ?? (_clips.Count > 0 ? _clips[0] : "");
    string PickWalk() => Match("walk", "run", "jog", "move", "step") ?? PickIdle();
    string PickWork() => (WorkAnimKey.Length > 0 ? Match(WorkAnimKey) : null)
                         ?? Match("work", "action", "cook", "serve", "prep", "use", "type") ?? PickIdle();

    // Exact (case-insensitive) clip name — needed to tell "sit" from "sitting".
    string? MatchExact(string name)
    {
        foreach (var c in _clips) if (c.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return c;
        return null;
    }

    // Resolve a state/greeting action to a clip, with synonyms; idle if absent.
    string PickAction(string key) => key switch
    {
        "sit"      => MatchExact("sit")     ?? Match("sit_down", "sitdown")        ?? PickIdle(),
        "sitting"  => MatchExact("sitting") ?? Match("seated", "seat", "chair")    ?? PickIdle(),
        "stand"    => MatchExact("stand")   ?? Match("stand_up", "standup", "rise")?? PickIdle(),
        "sweeping" => Match("sweep", "mop", "broom") ?? PickIdle(),
        "waving"   => Match("wav", "greet", "hello") ?? PickIdle(),
        _          => Match(key) ?? PickIdle(),
    };

    // ---- seated state: sit-down -> hold loop -> stand-up, timed by real clip length ----
    enum SeatState { None, Down, Sat, Up }
    SeatState _seat = SeatState.None;
    float _seatTimer;
    /// True while sitting or mid-transition; the agent must not walk during this.
    public bool Seated => _seat != SeatState.None;
    /// Ask to sit (true) or stand (false); transitions play their own clips.
    public void RequestSeated(bool wantSeated)
    {
        if (wantSeated && _seat == SeatState.None) { _seat = SeatState.Down; _seatTimer = ClipLen("sit", 1.2f); }
        else if (!wantSeated && (_seat == SeatState.Sat || _seat == SeatState.Down)) { _seat = SeatState.Up; _seatTimer = ClipLen("stand", 1.0f); }
    }
    float ClipLen(string exact, float fallback)
    {
        var c = MatchExact(exact);
        if (c != null && _anim != null) { var a = _anim.GetAnimation(c); if (a != null) return (float)a.Length; }
        return fallback;
    }

    /// Play a brief, non-looping action (e.g. a greeting wave) that overrides the
    /// normal state for `seconds`. Ignored if one is already playing (no spam).
    public void TriggerOneShot(string anim, float seconds)
    {
        if (_oneShotTimer > 0f) return;
        _oneShotAnim = anim;
        _oneShotTimer = seconds;
    }

    public override void _Process(double delta)
    {
        _t += (float)delta;
        // advance seat transitions regardless of render path so it can't get stuck
        if (_seat == SeatState.Down) { _seatTimer -= (float)delta; if (_seatTimer <= 0) _seat = SeatState.Sat; }
        else if (_seat == SeatState.Up) { _seatTimer -= (float)delta; if (_seatTimer <= 0) _seat = SeatState.None; }
        if (_usesModel)
        {
            if (_anim != null && _clips.Count > 0)
            {
                // Drive the model's own animation from agent state.
                if (_oneShotTimer > 0f) _oneShotTimer -= (float)delta;
                // Priority: walking > seated/transition > one-shot (wave) > action (sweep) > work > idle.
                string want = Moving ? PickWalk()
                            : _seat == SeatState.Down ? PickAction("sit")
                            : _seat == SeatState.Sat  ? PickAction("sitting")
                            : _seat == SeatState.Up   ? PickAction("stand")
                            : _oneShotTimer > 0f ? PickAction(_oneShotAnim)
                            : ActionAnim.Length > 0 ? PickAction(ActionAnim)
                            : Working ? PickWork()
                            : PickIdle();
                bool sweepScrub = ActionAnim == "sweeping" && !Moving && _seat == SeatState.None
                                  && _oneShotTimer <= 0f && Match("sweep", "mop", "broom") != null;
                if (sweepScrub)
                {
                    // (13) hold the sweep clip and scrub its first part back and forth by hand,
                    // so it reads as a short sweeping motion rather than a full swing.
                    if (want != _curClip) { _curClip = want; _anim.Play(want); }
                    _anim.Pause();
                    var sa = _anim.GetAnimation(want);
                    float len = sa != null ? (float)sa.Length : 1f;
                    float phase = (Mathf.Sin(_t * SweepRate) * 0.5f + 0.5f) * Mathf.Clamp(SweepStroke, 0.05f, 1f) * len;
                    _anim.Seek(phase, true);
                }
                else if (want.Length > 0 && want != _curClip)
                {
                    _curClip = want;
                    _anim.Play(want, 0.15);   // short crossfade between states
                }
            }
            else if (_boneDriven)
            {
                DriveBones();   // rigged but clip-less: swing bones directly
            }
            else if (_modelInstance != null)
            {
                // No clips, no usable rig: bob the whole body so walking isn't static.
                _modelInstance.Position = new Vector3(0, _modelBaseY + (Moving ? Mathf.Abs(Mathf.Sin(_t * 7.5f)) * 0.04f : 0f), 0);
            }
            return;
        }
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
    const float TurnRate = 12f;                 // higher = snappier turn (smoothed, not snapped)
    readonly System.Collections.Generic.List<Vector3> _path = new();
    int _pathIdx;
    Vector3 _navTarget = new(99999f, 0, 99999f);
    Vector3 _lastMovePos = new(99999f, 0, 99999f);
    float _stuckSeconds;

    /// Move toward a flat target, routing around obstacles via the navigation mesh
    /// when one is baked; falls back to a straight line if no path is available.
    /// Returns true on arrival. Facing is smoothed rather than snapped.
    public bool StepToward(Vector3 target, float delta)
    {
        target.Y = 0;
        target = ResolveOccupiedTarget(target, delta);
        var here = new Vector3(Position.X, 0, Position.Z);
        if ((here - target).LengthSquared() < 0.0144f) { Moving = false; _path.Clear(); return true; }  // ~0.12m

        var world = GetWorld3D();
        if (world != null)
        {
            if (_path.Count < 2 || _pathIdx >= _path.Count || (_navTarget - target).LengthSquared() > 0.25f)
            {
                var pts = NavigationServer3D.MapGetPath(world.NavigationMap, here, target, true);
                _path.Clear();
                if (pts != null) _path.AddRange(pts);
                _pathIdx = 0;
                _navTarget = target;
            }
            if (_path.Count >= 2)
            {
                bool atWp = MoveToward(_path[Mathf.Min(_pathIdx, _path.Count - 1)], delta);
                if (atWp && _pathIdx < _path.Count - 1) _pathIdx++;
                return false;   // final arrival is caught by the short-circuit above next frame
            }
        }
        return MoveToward(target, delta);   // no navmesh yet -> straight line
    }

    Vector3 ResolveOccupiedTarget(Vector3 target, float delta)
    {
        const float occupiedRadius = 0.82f;
        const float waitRadius = 1.05f;
        var here = new Vector3(Position.X, 0, Position.Z);
        CharacterRig? blocker = null;
        float bestSq = occupiedRadius * occupiedRadius;

        for (int i = 0; i < ActiveRigs.Count; i++)
        {
            var other = ActiveRigs[i];
            if (other == this || other == null || !IsInstanceValid(other)) continue;
            var otherPos = other.Position; otherPos.Y = 0;
            float dsq = (otherPos - target).LengthSquared();
            if (dsq < bestSq)
            {
                bestSq = dsq;
                blocker = other;
            }
        }

        if (blocker == null)
        {
            DestinationBlocked = false;
            DestinationBlockedSeconds = Mathf.Max(0f, DestinationBlockedSeconds - delta * 2f);
            return target;
        }

        var fromBlocker = target - new Vector3(blocker.Position.X, 0, blocker.Position.Z);
        if (fromBlocker.LengthSquared() < 0.01f) fromBlocker = here - target;
        if (fromBlocker.LengthSquared() < 0.01f)
        {
            float side = (GetInstanceId() & 1UL) == 0 ? 1f : -1f;
            fromBlocker = new Vector3(side, 0, -side);
        }

        DestinationBlocked = true;
        DestinationBlockedSeconds += delta;
        var baseDir = fromBlocker.Normalized();
        var lateral = new Vector3(baseDir.Z, 0, -baseDir.X);
        float lane = ((int)(GetInstanceId() % 5UL) - 2) * 0.28f;
        var wait = target + baseDir * waitRadius + lateral * lane;
        wait.Y = 0;
        return wait;
    }

    bool MoveToward(Vector3 target, float delta)
    {
        var pos = Position; pos.Y = 0; target.Y = 0;
        var d = target - pos;
        float dist = d.Length();
        if (dist < 0.12f) { Moving = false; return true; }
        Moving = true;
        if (_lastMovePos.X < 90000f)
        {
            float movedSq = (pos - _lastMovePos).LengthSquared();
            if (dist > 0.5f && movedSq < 0.0009f) _stuckSeconds += delta;
            else _stuckSeconds = Mathf.Max(0f, _stuckSeconds - delta * 2f);
        }
        _lastMovePos = pos;

        var desired = d.Normalized();
        var heading = AvoidedHeading(pos, desired);
        if (_stuckSeconds > 0.45f)
        {
            float side = (GetInstanceId() & 1UL) == 0 ? 1f : -1f;
            var lateral = new Vector3(desired.Z, 0, -desired.X) * side;
            float reverse = _stuckSeconds > 1.15f ? 0.65f : 0.25f;
            heading = (heading + lateral * 1.6f - desired * reverse).Normalized();
        }
        var step = heading * Mathf.Min(WalkSpeed * delta, dist);
        Position += step;
        float targetYaw = Mathf.Atan2(step.X, step.Z);
        // frame-rate-independent smooth turn toward heading
        Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, targetYaw, 1f - Mathf.Exp(-TurnRate * delta)), 0);
        return false;
    }

    Vector3 AvoidedHeading(Vector3 pos, Vector3 desired)
    {
        const float radius = 1.25f;
        const float radiusSq = radius * radius;
        var steer = desired;
        var lateral = new Vector3(desired.Z, 0, -desired.X);

        for (int i = 0; i < ActiveRigs.Count; i++)
        {
            var other = ActiveRigs[i];
            if (other == this || other == null || !IsInstanceValid(other)) continue;
            var otherPos = other.Position; otherPos.Y = 0;
            var away = pos - otherPos;
            float distSq = away.LengthSquared();
            if (distSq > radiusSq) continue;

            if (distSq < 0.0001f)
            {
                float side = ((GetInstanceId() + other.GetInstanceId()) & 1UL) == 0 ? 1f : -1f;
                steer += lateral * side * 0.9f;
                continue;
            }

            float dist = Mathf.Sqrt(distSq);
            float weight = 1f - dist / radius;
            float sideSign = away.Dot(lateral) >= 0f ? 1f : -1f;
            var awayDir = away / dist;
            bool walkingIntoOther = desired.Dot(-awayDir) > 0.35f;
            bool faceToFace = Moving && other.Moving && desired.Dot(other.GlobalTransform.Basis.Z) > 0.35f;

            // Blend a sidestep with a smaller personal-space push so agents route
            // around one another instead of pressing straight into the overlap.
            steer += lateral * sideSign * weight * ((walkingIntoOther || faceToFace) ? 2.65f : 1.35f);
            steer += awayDir * weight * (walkingIntoOther ? 0.8f : 0.45f);
            if ((walkingIntoOther || faceToFace) && dist < 0.9f) steer -= desired * weight * 0.95f;
        }

        return steer.LengthSquared() > 0.0001f ? steer.Normalized() : desired;
    }

    /// Stationary characters still need personal-space behavior. Without this,
    /// queued/waiting customers become fixed blockers while moving agents steer.
    public void HoldWithPersonalSpace(float delta)
    {
        const float radius = 1.25f;
        const float minGap = 0.78f;
        var pos = Position; pos.Y = 0f;
        var push = Vector3.Zero;
        for (int i = 0; i < ActiveRigs.Count; i++)
        {
            var other = ActiveRigs[i];
            if (other == this || other == null || !IsInstanceValid(other)) continue;
            var otherPos = other.Position; otherPos.Y = 0f;
            var away = pos - otherPos;
            float distSq = away.LengthSquared();
            if (distSq > radius * radius) continue;

            if (distSq < 0.0001f)
            {
                float side = ((GetInstanceId() + other.GetInstanceId()) & 1UL) == 0 ? 1f : -1f;
                push += new Vector3(side, 0f, -side) * 0.35f;
                continue;
            }

            float dist = Mathf.Sqrt(distSq);
            var awayDir = away / dist;
            float weight = 1f - dist / radius;
            push += awayDir * weight;
            if (dist < minGap) push += awayDir * ((minGap - dist) * 1.4f);
        }

        if (push.LengthSquared() < 0.0001f)
        {
            Moving = false;
            return;
        }

        var heading = push.Normalized();
        Position += heading * Mathf.Min(WalkSpeed * 0.55f * delta, 0.06f);
        float targetYaw = Mathf.Atan2(heading.X, heading.Z);
        Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, targetYaw, 1f - Mathf.Exp(-TurnRate * delta)), 0);
        Moving = false;
    }

    /// Smoothly turn to face a point without moving (used while standing at a station).
    public void FaceToward(Vector3 point, float delta)
    {
        var d = point - Position; d.Y = 0;
        if (d.LengthSquared() < 1e-4f) return;
        float yaw = Mathf.Atan2(d.X, d.Z);
        Rotation = new Vector3(0, Mathf.LerpAngle(Rotation.Y, yaw, 1f - Mathf.Exp(-TurnRate * delta)), 0);
    }
}
