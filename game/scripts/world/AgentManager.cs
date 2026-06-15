using Godot;
using System.Collections.Generic;

namespace RestaurantSimulator;

/// Bridges simulation events to animated characters and vehicles. Strictly
/// presentation-layer: it subscribes to SimRunState events and reads public
/// state, but never writes back, so visuals cannot break deterministic replay.
public partial class AgentManager : Node3D
{
    SimRunState _sim = null!;
    WorldLayout _world = null!;
    readonly System.Random _vis = new(424242);   // visual-only variety

    readonly List<CustomerAgent> _walkins = new();
    readonly List<CarAgent> _cars = new();
    readonly Dictionary<string, Node> _byOrder = new();
    readonly List<EmployeeAgent> _staff = new();
    string _staffSignature = "";

    const int MaxWalkins = 16, MaxCars = 8;
    const int BoardIdx = 2, WindowIdx = 4;

    // RS-VS-002: imported staff models live here. The three knobs align the GLBs
    // to the procedural world — tune in-editor, then rebuild.
    const string StaffModelDir = "res://models/staff/";
    public static float StaffModelScale = 1f;
    public static float StaffModelYaw = 0f;      // facing offset; flip to 180 if they face away
    public static float StaffModelYOffset = 0.1f;  // floor top sits at ~0.08–0.11; lifts feet onto it

    static readonly Color[] CarPaints =
    {
        new(0.75f,0.1f,0.1f), new(0.12f,0.25f,0.55f), new(0.85f,0.85f,0.88f),
        new(0.15f,0.15f,0.17f), new(0.5f,0.5f,0.55f), new(0.7f,0.5f,0.15f), new(0.2f,0.45f,0.3f)
    };
    static readonly Color[] Shirts =
    {
        new(0.2f,0.4f,0.7f), new(0.6f,0.5f,0.3f), new(0.3f,0.55f,0.35f),
        new(0.55f,0.3f,0.5f), new(0.8f,0.7f,0.3f), new(0.4f,0.4f,0.45f)
    };
    static readonly Color[] Skins =
    {
        new(0.95f,0.8f,0.66f), new(0.85f,0.65f,0.5f), new(0.6f,0.42f,0.3f), new(0.45f,0.3f,0.22f)
    };

    public void Init(SimRunState sim, WorldLayout world)
    {
        _sim = sim;
        _world = world;
        _sim.OrderCreatedEvt += OnOrder;
        _sim.TicketCompletedEvt += OnTicketDone;
        _sim.TicketAbandonedEvt += OnTicketDone;   // RS-HQ-001: guest gives up and leaves (same release path)
    }

    // ---------------- spawning ----------------

    void OnOrder(string channel, string orderId)
    {
        switch (channel)
        {
            case "drive_thru": SpawnCar(orderId); break;
            case "lobby": SpawnWalkin(orderId, channel, fromDoor: true); GreetManagers(); break;
            case "mobile": SpawnWalkin(orderId, channel, fromDoor: true); GreetManagers(); break;
            case "delivery": SpawnWalkin(orderId, channel, fromDoor: true, courier: true); break;
        }
    }

    // Store manager (asst mgr / GM model) waves when a guest comes through the door.
    void GreetManagers()
    {
        var entry = _world.Anchor["door"];   // where customers come in
        foreach (var e in _staff)
            if (e.Role == "assistant_manager" || e.Role == "restaurant_manager")
                e.GoGreet(entry);
    }

    // Patrol stops for the GM — work spots around the store, in a sensible loop.
    System.Collections.Generic.List<Vector3> PatrolWaypoints()
    {
        var keys = new[] { "work_grill", "work_assembly", "work_expo", "work_counter",
                           "work_dt", "work_office", "work_fryer", "work_prep" };
        var list = new System.Collections.Generic.List<Vector3>();
        foreach (var k in keys)
            if (_world.Anchor.TryGetValue(k, out var p)) list.Add(p);
        return list;
    }

    void SpawnCar(string orderId)
    {
        if (_cars.Count >= MaxCars) return;
        var car = new CarAgent { OrderId = orderId, Lane = _world.DtLane };
        car.BuildCar(CarPaints[_vis.Next(CarPaints.Length)], _vis.Next(3));
        car.Position = _world.DtLane[0];
        car.Ahead = _cars.Count > 0 ? _cars[^1] : null;
        AddChild(car);
        _cars.Add(car);
        _byOrder[orderId] = car;
    }

    void SpawnWalkin(string orderId, string channel, bool fromDoor, bool courier = false)
    {
        if (_walkins.Count >= MaxWalkins) return;
        var a = new CustomerAgent { OrderId = orderId, Channel = channel, Courier = courier };
        var shirt = courier ? new Color(0.9f, 0.45f, 0.1f) : Shirts[_vis.Next(Shirts.Length)];
        a.BuildHuman(shirt, new Color(0.2f, 0.2f, 0.25f), Skins[_vis.Next(Skins.Length)],
            courier ? new Color(0.9f, 0.45f, 0.1f) : null,
            0.92f + (float)_vis.NextDouble() * 0.16f);   // RS-VS-001 height variance
        a.Position = _world.Anchor["door_out"] + new Vector3((float)(_vis.NextDouble() * 2 - 1), 0, -0.6f);  // on the sidewalk (navmesh), not out in the lot
        int q = System.Math.Min(CountQueued(), _world.QueueSpots.Count - 1);
        a.QueueSpot = channel == "lobby"
            ? _world.QueueSpots[q]
            : _world.Anchor["mobile_wait"] + new Vector3((float)(_vis.NextDouble() * 1.6f - 0.8f), 0, 0.3f);
        a.PickupSpot = channel == "lobby" ? _world.Anchor["pickup"] : _world.Anchor["mobile_shelf"] + new Vector3(0, 0, 1.0f);
        bool dines = channel == "lobby" && !courier && _vis.NextDouble() < 0.45 && _world.Tables.Count > 0;
        a.TableSpot = dines ? _world.Tables[_vis.Next(_world.Tables.Count)] : Vector3.Zero;
        a.ExitSpot = _world.Anchor["door_out"] + new Vector3((float)(_vis.NextDouble() * 2 - 1), 0, -0.8f);  // reachable point on the sidewalk so they actually despawn
        a.Configure(orderPause: channel == "lobby" ? 6f : 0f, dineSeconds: dines ? 25f + _vis.Next(40) : 0f);
        AddChild(a);
        _walkins.Add(a);
        _byOrder[orderId] = a;
    }

    int CountQueued()
    {
        int n = 0;
        foreach (var a in _walkins)
            if (a.Channel == "lobby" && (a.State == CustomerAgent.Phase.Enter ||
                a.State == CustomerAgent.Phase.Ordering || a.State == CustomerAgent.Phase.Waiting)) n++;
        return n;
    }

    void OnTicketDone(string channel, string orderId)
    {
        if (!_byOrder.TryGetValue(orderId, out var node)) return;
        if (node is CustomerAgent c) c.TicketDone = true;
        if (node is CarAgent car) car.TicketDone = true;
    }

    // ---------------- per-frame ----------------

    public override void _Process(double delta)
    {
        float d = (float)delta;

        for (int i = _walkins.Count - 1; i >= 0; i--)
            if (_walkins[i].Drive(d)) Free(_walkins[i], i);

        for (int i = _cars.Count - 1; i >= 0; i--)
            if (_cars[i].Drive(d, BoardIdx, WindowIdx))
            {
                _byOrder.Remove(_cars[i].OrderId);
                _cars[i].QueueFree();
                _cars.RemoveAt(i);
                for (int j = 0; j < _cars.Count; j++) _cars[j].Ahead = j == 0 ? null : _cars[j - 1];
            }

        SyncStaff(d);
    }

    void Free(CustomerAgent a, int idx)
    {
        _byOrder.Remove(a.OrderId);
        a.QueueFree();
        _walkins.RemoveAt(idx);
    }

    // ---------------- staffing visuals ----------------

    void SyncStaff(float d)
    {
        var spec = BuildStaffSpec();
        var sb = new System.Text.StringBuilder();
        foreach (var (st, role) in spec) sb.Append(st).Append(':').Append(role).Append('|');
        string sig = sb.ToString();
        if (sig != _staffSignature)
        {
            _staffSignature = sig;
            foreach (var e in _staff) e.QueueFree();
            _staff.Clear();
            int i = 0;
            foreach (var (key, role) in spec)
            {
                var e = new EmployeeAgent { StationKey = key, Role = role };
                // RS-VS-002 role -> model. crew + team leaders share employee_m/f
                // (stable pick per slot); managers each get their own model.
                string file = role switch
                {
                    "shift_manager"      => "shift_manager.glb",
                    "assistant_manager"  => "store_manager.glb",          // shares the GM model (no asst-mgr asset)
                    "restaurant_manager" => "store_manager.glb",          // GM
                    _                    => (((i * 2654435761u) >> 16) & 1) == 0 ? "employee_m.glb" : "employee_f.glb",
                };
                if (!e.BuildModel(StaffModelDir + file, StaffModelScale, StaffModelYaw, StaffModelYOffset))
                {
                    // procedural fallback keeps the agent visible if a GLB is missing
                    var shirt = role == "crew" ? new Color(0.75f, 0.16f, 0.12f)
                              : role == "shift_manager" ? new Color(0.92f, 0.92f, 0.92f)
                              : role == "assistant_manager" ? new Color(0.80f, 0.80f, 0.85f)
                              : new Color(0.10f, 0.10f, 0.12f);   // restaurant_manager / GM
                    e.BuildHuman(shirt, new Color(0.12f, 0.12f, 0.15f), Skins[i % Skins.Length], new Color(0.75f, 0.16f, 0.12f));
                }
                e.HomeSpot = _world.Anchor[key] + new Vector3((i % 3) * 0.55f - 0.55f, 0, 0);
                e.FaceTarget = FaceFor(key, e.HomeSpot);
                e.HasFace = true;
                if (role == "assistant_manager" || role == "restaurant_manager")
                {
                    e.Patrols = true;                       // GM walks the floor checking workflow
                    e.PatrolRoute = PatrolWaypoints();
                }
                e.CoolerSpot = _world.Anchor["cooler"] + new Vector3(1.3f + (i % 3) * 0.7f, 0, 0.4f + (i % 2) * 0.6f);  // staggered so they do not pile up
                e.BreakSpot = _world.Anchor["break_room"] + new Vector3((i % 3) * 0.6f - 0.6f, 0, 0);
                e.Position = e.HomeSpot;
                e.Init(i);
                AddChild(e);
                _staff.Add(e);
                i++;
            }
        }

        int onBreak = _sim.CrewOnBreak;
        for (int k = 0; k < _staff.Count; k++)
        {
            var e = _staff[k];
            e.OnBreak = k < onBreak && e.Role == "crew";   // managers don't take breaks
            e.Drive(d, StationBusy(e.StationKey));
        }
    }

    // Where a staffer at `workKey` should look while standing at their station —
    // toward the equipment they operate (or the counter/lobby for front of house).
    Vector3 FaceFor(string workKey, Vector3 home)
    {
        string? id = workKey switch
        {
            "work_grill" => "grill", "work_fryer" => "fryer", "work_prep" => "prep",
            "work_assembly" => "assembly", "work_beverage" => "beverage", "work_expo" => "expo",
            "work_dt" => "dt_window", "work_office" => "office", _ => null
        };
        if (id != null && _world.Anchor.TryGetValue(id, out var p)) return p;
        if (workKey == "work_counter") return new Vector3(home.X, 0, -0.2f);   // face the counter / lobby
        return home + new Vector3(0, 0, -1f);
    }

    List<(string Station, string Role)> BuildStaffSpec()
    {
        var spec = new List<(string, string)>();
        string[] kitchenCycle = { "work_grill", "work_assembly", "work_expo" };
        for (int i = 0; i < _sim.KitchenCoverage; i++) spec.Add((kitchenCycle[i % kitchenCycle.Length], "crew"));
        for (int i = 0; i < _sim.FryerCoverage; i++) spec.Add(("work_fryer", "crew"));
        string[] driveCycle = { "work_dt", "work_beverage" };
        for (int i = 0; i < _sim.DriveCoverage; i++) spec.Add((driveCycle[i % driveCycle.Length], "crew"));
        for (int i = 0; i < _sim.CounterCoverage; i++) spec.Add(("work_counter", "crew"));
        for (int i = 0; i < _sim.PrepCoverage; i++) spec.Add(("work_prep", "crew"));
        // Managers each get their own model by role.
        for (int i = 0; i < _sim.ShiftMgr; i++) spec.Add(("work_expo", "shift_manager"));  // shift mgr expedites
        for (int i = 0; i < _sim.AsstMgr; i++) spec.Add(("work_office", "assistant_manager"));
        for (int i = 0; i < _sim.RestMgr; i++) spec.Add(("work_office", "restaurant_manager"));
        // A store manager (asst mgr or GM) always runs the shift; stand one in if the roster has neither.
        if (_sim.AsstMgr == 0 && _sim.RestMgr == 0) spec.Add(("work_office", "assistant_manager"));
        return spec;
    }

    bool StationBusy(string key) => key switch
    {
        "work_grill" => _sim.GrillLoad > 0,
        "work_fryer" => _sim.FryerLoad > 0,
        "work_assembly" => _sim.AssemblyLoad > 0,
        "work_beverage" or "work_expo" => _sim.ExpoLoad > 0,
        "work_prep" => _sim.Prep < 100,
        "work_counter" or "work_dt" => _sim.Tickets > 0,
        _ => false
    };
}
