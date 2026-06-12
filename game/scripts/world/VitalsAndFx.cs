using Godot;
using System.Collections.Generic;
using System.Globalization;

namespace RestaurantSimulator;

/// RS-VS-001: reads the kitchen at a glance — floating KDS vitals above the line,
/// hold-pan levels with age, blinking overload beacons, and fryer steam.
public partial class VitalsAndFx : Node3D
{
    SimRunState _sim = null!;
    WorldLayout _world = null!;
    readonly Dictionary<string, Label3D> _boards = new();
    readonly Dictionary<string, MeshInstance3D> _beacons = new();
    CpuParticles3D _steam = null!;
    float _accum, _pulse;

    public void Init(SimRunState sim, WorldLayout world)
    {
        _sim = sim;
        _world = world;

        foreach (var id in new[] { "grill", "fryer", "assembly", "expo", "beverage" })
        {
            if (!_world.Anchor.TryGetValue(id, out var at)) continue;

            var board = new Label3D
            {
                FontSize = 40,
                Position = at + new Vector3(0, 2.05f, 0),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                Modulate = new Color(0.65f, 1f, 0.75f),
                OutlineSize = 8,
                Name = "kds_" + id
            };
            AddChild(board);
            _boards[id] = board;

            var beacon = new MeshInstance3D
            {
                Mesh = new SphereMesh { Radius = 0.12f, Height = 0.24f },
                Position = at + new Vector3(0, 2.55f, 0),
                Visible = false,
                MaterialOverride = new StandardMaterial3D
                {
                    AlbedoColor = new Color(1f, 0.15f, 0.1f),
                    EmissionEnabled = true,
                    Emission = new Color(1f, 0.15f, 0.1f),
                    EmissionEnergyMultiplier = 2.0f
                },
                Name = "beacon_" + id
            };
            AddChild(beacon);
            _beacons[id] = beacon;
        }

        // Fryer steam — emits while the fryer line is actually working.
        if (_world.Anchor.TryGetValue("fryer", out var fry))
        {
            _steam = new CpuParticles3D
            {
                Position = fry + new Vector3(0, 1.15f, 0),
                Amount = 14,
                Lifetime = 1.3f,
                Mesh = new SphereMesh { Radius = 0.035f, Height = 0.07f },
                Direction = new Vector3(0, 1, 0),
                Spread = 12f,
                InitialVelocityMin = 0.35f,
                InitialVelocityMax = 0.7f,
                Gravity = new Vector3(0, 0.5f, 0),
                Emitting = false,
                Name = "steam_fryer"
            };
            AddChild(_steam);
        }
    }

    public override void _Process(double delta)
    {
        _pulse += (float)delta;
        _accum += (float)delta;
        if (_accum < 0.25f) return;
        _accum = 0;

        var inv = CultureInfo.InvariantCulture;
        Set("grill", string.Format(inv, "GRILL {0:0.0}m | HOLD {1:0}/{2:0} {3}",
            _sim.GrillBacklogMinutes, _sim.HoldLevel("grilled_main"), _sim.HoldCapacity("grilled_main"), Age("grilled_main")));
        Set("fryer", string.Format(inv, "FRYER {0:0.0}m | FRIED {1:0}/{2:0} {3} | FRIES {4:0}/{5:0} {6}",
            _sim.FryerBacklogMinutes, _sim.HoldLevel("fried_main"), _sim.HoldCapacity("fried_main"), Age("fried_main"),
            _sim.HoldLevel("fries"), _sim.HoldCapacity("fries"), Age("fries")));
        Set("assembly", string.Format(inv, "ASSEMBLY {0:0.0}m", _sim.AssemblyBacklogMinutes));
        Set("expo", string.Format(inv, "EXPO {0:0.0}m | BOARD {1}", _sim.ExpoBacklogMinutes, _sim.Tickets));
        Set("beverage", string.Format(inv, "BEV | CSAT {0:0.0}", _sim.Csat));

        foreach (var (id, board) in _boards)
        {
            bool warn = id switch
            {
                "grill" => _sim.GrillBacklogMinutes > 6 || _sim.HoldLevel("grilled_main") <= 0,
                "fryer" => _sim.FryerBacklogMinutes > 6 || _sim.HoldLevel("fried_main") <= 0 || _sim.HoldLevel("fries") <= 0,
                "assembly" => _sim.AssemblyBacklogMinutes > 6,
                "expo" => _sim.Tickets > 14,
                _ => false
            };
            board.Modulate = warn ? new Color(1f, 0.55f, 0.4f) : new Color(0.65f, 1f, 0.75f);
        }

        var overloaded = _sim.StationOverloaded ? _sim.ActiveOverloadStation : "";
        foreach (var (id, beacon) in _beacons)
        {
            var on = overloaded == id;
            beacon.Visible = on && (_pulse % 0.6f) < 0.35f;
        }

        if (_steam != null)
            _steam.Emitting = _sim.Running && (_sim.FryerLoad > 0 || _sim.HoldInFlight("fried_main") + _sim.HoldInFlight("fries") > 0);
    }

    void Set(string id, string text) { if (_boards.TryGetValue(id, out var b)) b.Text = text; }

    string Age(string family)
    {
        var m = _sim.HoldOldestAgeMin(family);
        return m <= 0 ? "" : string.Format(CultureInfo.InvariantCulture, "{0:0}:{1:00}", (int)m, (int)((m % 1) * 60));
    }
}
