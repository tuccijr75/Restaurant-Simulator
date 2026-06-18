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
    readonly Dictionary<string, Node3D> _parkedCars = new();
    readonly Dictionary<string, Node> _byOrder = new();
    readonly List<EmployeeAgent> _staff = new();
    string _staffSignature = "";
    bool _shutdown;

    // RS-ST-002/003: the roster is the single source of truth for who is on the
    // floor, where they stand, and their evolving stats. Built once from the career
    // week seed; RosterDay selects which day's schedule (set from CareerState.DayIndex
    // when plumbed — defaults to 0, still a full valid schedule).
    Roster _roster = null!;
    public static int RosterWeekSeed = 777001;
    public int RosterDay = 0;
    int _lastStatMinute = -1;

    const int MaxWalkins = 16, MaxCars = 8;
    const int BoardIdx = 2, WindowIdx = 4;

    // RS-VS-002: imported staff models live here. The three knobs align the GLBs
    // to the procedural world — tune in-editor, then rebuild.
    const string StaffModelDir = "res://models/staff/";
    const string CustomerModelDir = "res://models/customers/";
    public static float StaffModelScale = 1f;
    public static float StaffModelYaw = 0f;      // facing offset; flip to 180 if they face away
    public static float StaffModelYOffset = 0.1f;  // floor top sits at ~0.08–0.11; lifts feet onto it
    // (6) customer GLBs render smaller than the staff GLB — raise this until customers match staff height.
    public static float CustomerModelScale = 1.5f;

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
        _roster = new Roster(RosterWeekSeed);
    }

    public override void _ExitTree()
    {
        Shutdown();
    }

    public void Shutdown()
    {
        if (_shutdown) return;
        _shutdown = true;
        if (_sim != null)
        {
            _sim.OrderCreatedEvt -= OnOrder;
            _sim.TicketCompletedEvt -= OnTicketDone;
            _sim.TicketAbandonedEvt -= OnTicketDone;
        }
        foreach (var a in _walkins) if (a != null && IsInstanceValid(a)) a.QueueFree();
        foreach (var c in _cars) if (c != null && IsInstanceValid(c)) c.QueueFree();
        foreach (var c in _parkedCars.Values) if (c != null && IsInstanceValid(c)) c.QueueFree();
        foreach (var e in _staff) if (e != null && IsInstanceValid(e)) e.QueueFree();
        _walkins.Clear();
        _cars.Clear();
        _parkedCars.Clear();
        _byOrder.Clear();
        _staff.Clear();
        _staffSignature = "";
    }

    // Read-only hooks for the staff UI (#10 schedule panel, #12 click-inspect).
    public System.Collections.Generic.IReadOnlyList<EmployeeAgent> Staff => _staff;
    public Roster StaffRoster => _roster;
    public int SimMinute => (int)_sim.Minute;

    // ---------------- spawning ----------------

    void OnOrder(string channel, string orderId)
    {
        switch (channel)
        {
            case "drive_thru": SpawnCar(orderId); break;
            case "lobby": GreetManagers(SpawnWalkin(orderId, channel, fromDoor: true)); break;
            case "mobile": GreetManagers(SpawnWalkin(orderId, channel, fromDoor: true)); break;
            case "delivery": SpawnWalkin(orderId, channel, fromDoor: true, courier: true); break;
        }
    }

    // Store manager (asst mgr / GM model) waves when a guest comes through the door.
    void GreetManagers(CustomerAgent? guest)
    {
        if (guest == null) return;
        foreach (var e in _staff)
            if (e.Role == "assistant_manager" || e.Role == "restaurant_manager")
                e.GoGreet(guest);   // wave at the actual customer, not the doorway
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
        bool truck = _vis.Next(4) == 0;   // ~25% pickups
        if (!car.BuildCarModel("res://models/vehicles/" + (truck ? "truck.glb" : "car.glb")))
            car.BuildCar(CarPaints[_vis.Next(CarPaints.Length)], _vis.Next(3));
        car.Position = _world.DtLane[0];
        car.Ahead = _cars.Count > 0 ? _cars[^1] : null;
        AddChild(car);
        _cars.Add(car);
        _byOrder[orderId] = car;
    }

    CustomerAgent? SpawnWalkin(string orderId, string channel, bool fromDoor, bool courier = false)
    {
        if (_walkins.Count >= MaxWalkins) return null;
        var a = new CustomerAgent { OrderId = orderId, Channel = channel, Courier = courier };
        var shirt = courier ? new Color(0.9f, 0.45f, 0.1f) : Shirts[_vis.Next(Shirts.Length)];
        float scale = CustomerModelScale * (0.95f + (float)_vis.NextDouble() * 0.12f);   // RS-VS-001 height variance
        string custFile = (_vis.Next(2) == 0 ? "customer_m.glb" : "customer_f.glb");
        if (courier || !a.BuildModel(CustomerModelDir + custFile, scale, 0f, 0.1f))
        {
            // couriers and any missing-model case fall back to the procedural human
            a.BuildHuman(shirt, new Color(0.2f, 0.2f, 0.25f), Skins[_vis.Next(Skins.Length)],
                courier ? new Color(0.9f, 0.45f, 0.1f) : null,
                0.92f + (float)_vis.NextDouble() * 0.16f);
        }
        a.Position = _world.Anchor["door_out"] + new Vector3((float)(_vis.NextDouble() * 2 - 1), 0, -0.6f);  // on the sidewalk (navmesh), not out in the lot
        var parkingSpot = SelectParkingSpot(channel, courier);
        if (parkingSpot.HasValue)
        {
            a.Position = parkingSpot.Value + new Vector3((float)(_vis.NextDouble() * 0.5f - 0.25f), 0, 0.15f);
            SpawnParkedCar(orderId, parkingSpot.Value);
        }
        int q = System.Math.Min(CountQueued(), _world.QueueSpots.Count - 1);
        a.QueueSpot = channel == "lobby"
            ? _world.QueueSpots[q]
            : _world.Anchor["mobile_wait"] + new Vector3((float)(_vis.NextDouble() * 1.6f - 0.8f), 0, 0.3f);
        a.PickupSpot = channel == "lobby" ? _world.Anchor["pickup"] : _world.Anchor["mobile_shelf"] + new Vector3(0, 0, 1.0f);
        bool dines = channel == "lobby" && !courier && _vis.NextDouble() < 0.45 && _world.Tables.Count > 0;
        a.TableSpot = dines ? _world.Tables[_vis.Next(_world.Tables.Count)] : Vector3.Zero;
        a.BusSpot = _world.Anchor["pickup"] + new Vector3((float)(_vis.NextDouble() * 1.2 - 0.6), 0, 0.5f);  // #6 return the tray to the counter
        a.ExitSpot = parkingSpot ?? _world.Anchor["door_out"] + new Vector3((float)(_vis.NextDouble() * 2 - 1), 0, -0.8f);  // reachable point so they actually despawn
        a.Configure(orderPause: channel == "lobby" ? 6f : 0f, dineSeconds: dines ? 25f + _vis.Next(40) : 0f);
        AddChild(a);
        _walkins.Add(a);
        _byOrder[orderId] = a;
        return a;
    }

    Vector3? SelectParkingSpot(string channel, bool courier)
    {
        if (channel != "lobby" || courier || _world.ParkingSpots.Count == 0 || _vis.NextDouble() > 0.6) return null;

        int start = _vis.Next(_world.ParkingSpots.Count);
        for (int i = 0; i < _world.ParkingSpots.Count; i++)
        {
            var spot = _world.ParkingSpots[(start + i) % _world.ParkingSpots.Count];
            bool occupied = false;
            foreach (var parked in _parkedCars.Values)
            {
                if (parked != null && IsInstanceValid(parked) && parked.Position.DistanceSquaredTo(CarSpotFor(spot)) < 1.0f)
                {
                    occupied = true;
                    break;
                }
            }
            if (!occupied) return spot;
        }
        return null;
    }

    void SpawnParkedCar(string orderId, Vector3 walkSpot)
    {
        var car = new CarAgent();
        bool truck = _vis.Next(4) == 0;
        if (!car.BuildCarModel("res://models/vehicles/" + (truck ? "truck.glb" : "car.glb")))
            car.BuildCar(CarPaints[_vis.Next(CarPaints.Length)], _vis.Next(3));
        car.Position = CarSpotFor(walkSpot);
        car.RotationDegrees = new Vector3(0, 180f, 0);
        AddChild(car);
        _parkedCars[orderId] = car;
    }

    static Vector3 CarSpotFor(Vector3 walkSpot) => walkSpot + new Vector3(0, 0, 1.7f);

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
        Separate();
    }

    // RS-VS-001 agent avoidance: nudge overlapping characters apart each frame so
    // staff and customers stop clipping through one another. O(n^2) but n is small
    // (~20-40 agents); a small push off the navmesh is re-pathed next frame.
    void Separate()
    {
        var all = new List<Node3D>(_staff.Count + _walkins.Count);
        all.AddRange(_staff);
        all.AddRange(_walkins);
        const float minGap = 0.62f;                 // personal-space radius (sum of half-widths)
        for (int i = 0; i < all.Count; i++)
        {
            for (int j = i + 1; j < all.Count; j++)
            {
                var a = all[i]; var b = all[j];
                var delta = a.Position - b.Position; delta.Y = 0f;
                float dist = delta.Length();
                if (dist > 1e-3f && dist < minGap)
                {
                    var push = delta / dist * ((minGap - dist) * 0.5f);   // each moves half the overlap
                    a.Position += push;
                    b.Position -= push;
                }
                else if (dist <= 1e-3f)                                    // exactly stacked: deterministic nudge
                {
                    var jitter = new Vector3(((i * 13 + j) % 7 - 3) * 0.05f, 0f, ((i * 7 + j) % 5 - 2) * 0.05f);
                    a.Position += jitter;
                }
            }
        }
    }

    void Free(CustomerAgent a, int idx)
    {
        _byOrder.Remove(a.OrderId);
        if (_parkedCars.TryGetValue(a.OrderId, out var car))
        {
            if (car != null && IsInstanceValid(car)) car.QueueFree();
            _parkedCars.Remove(a.OrderId);
        }
        a.QueueFree();
        _walkins.RemoveAt(idx);
    }

    // ---------------- staffing visuals ----------------

    void SyncStaff(float d)
    {
        int minute = (int)_sim.Minute;
        var spec = BuildStaffSpec(minute);

        // Membership signature: who is on shift and where. Stable across a shift, so
        // agents are rebuilt only at shift boundaries (breaks don't churn the roster).
        var sb = new System.Text.StringBuilder();
        foreach (var s in spec) sb.Append(s.EmpId).Append(':').Append(s.Station).Append('|');
        string sig = sb.ToString();
        if (sig != _staffSignature)
        {
            _staffSignature = sig;
            foreach (var e in _staff) e.QueueFree();
            _staff.Clear();
            int i = 0;
            foreach (var (key, role, empId, name) in spec)
            {
                var e = new EmployeeAgent
                {
                    StationKey = key, Role = role,
                    EmpId = empId, EmpName = name, Assigned = StationLabel(key),
                };
                // RS-VS-002 role -> model. crew + team leaders share employee_m/f
                // (stable pick per slot); managers each get their own model.
                string file = role switch
                {
                    "shift_manager"      => "shift_manager.glb",
                    "assistant_manager"  => "store_manager.glb",          // shares the GM model (no asst-mgr asset)
                    "restaurant_manager" => "store_manager.glb",          // GM
                    _                    => (((empId * 2654435761u) >> 16) & 1) == 0 ? "employee_m.glb" : "employee_f.glb",
                };
                if (!e.BuildModel(StaffModelDir + file, StaffModelScale, StaffModelYaw, StaffModelYOffset))
                {
                    var shirt = role == "crew" ? new Color(0.75f, 0.16f, 0.12f)
                              : role == "shift_manager" ? new Color(0.92f, 0.92f, 0.92f)
                              : role == "assistant_manager" ? new Color(0.80f, 0.80f, 0.85f)
                              : new Color(0.10f, 0.10f, 0.12f);   // restaurant_manager / GM
                    e.BuildHuman(shirt, new Color(0.12f, 0.12f, 0.15f), Skins[i % Skins.Length], new Color(0.75f, 0.16f, 0.12f));
                }
                e.HomeSpot = _world.Anchor[key] + new Vector3((i % 3) * 0.55f - 0.55f, 0, 0);
                e.FaceTarget = FaceFor(key, e.HomeSpot);
                e.HasFace = true;
                if ((role == "assistant_manager" || role == "restaurant_manager") && key == "work_office")
                {
                    e.Patrols = true;                       // floor managers walk the floor between critical fills
                    e.PatrolRoute = PatrolWaypoints();
                }
                if (key == "work_counter" || key == "work_counter2")
                {
                    e.IsCashier = true;                     // step up to the POS and serve when a customer is ordering
                    e.ServeSpot = e.HomeSpot + new Vector3(0, 0, 0.55f);
                }
                e.CoolerSpot = _world.Anchor["freezer_door"] + new Vector3((i % 3) * 0.6f - 0.6f, 0, 0.3f + (i % 2) * 0.4f);
                e.BreakSpot = _world.Anchor["break_room"] + new Vector3((i % 3) * 0.6f - 0.6f, 0, 0);
                e.Position = e.HomeSpot;
                e.Init(i);
                AddChild(e);
                _staff.Add(e);
                i++;
            }
        }

        // Per-frame: break state straight from the roster (drives walk-to-break-room),
        // then advance each agent.
        foreach (var e in _staff) e.OnBreak = false;
        foreach (var (sh, onBreak) in _roster.PresentAt(RosterDay, minute))
        {
            if (!onBreak) continue;
            for (int k = 0; k < _staff.Count; k++)
                if (_staff[k].EmpId == sh.EmployeeId) { _staff[k].OnBreak = true; break; }
        }
        foreach (var e in _staff) e.Drive(d, StationBusy(e.StationKey));

        // Advance employee stats once per sim-minute (catch up small deltas; ignore
        // the backward jump at day rollover).
        if (_lastStatMinute < 0 || minute < _lastStatMinute) { _lastStatMinute = minute; return; }
        int dt = minute - _lastStatMinute;
        if (dt > 0)
        {
            _roster.TickStats(RosterDay, minute, System.Math.Min(dt, 10), StorePerf(), StationLoad01);
            _lastStatMinute = minute;
        }
    }

    // How the store is running, normalized so 1.0 = a normal day. Above 1 lifts
    // manager attitude (which lifts crew); below 1 drags it. Feeds Roster.TickStats.
    float StorePerf()
    {
        double v = 0.55 * (_sim.SalesPacePercent / 100.0)
                 + 0.45 * (_sim.Csat / 90.0)
                 - (_sim.StationOverloaded ? 0.15 : 0.0);
        return (float)System.Math.Clamp(v, 0.4, 1.6);
    }

    // Station busyness in [0,1] — the stress driver at each post. Caps are rough and
    // tunable; loads are the same public counters StationBusy reads.
    static float C01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
    float StationLoad01(string key) => key switch
    {
        "work_grill"    => C01(_sim.GrillLoad / 6f),
        "work_fryer"    => C01(_sim.FryerLoad / 6f),
        "work_assembly" => C01(_sim.AssemblyLoad / 6f),
        "work_expo" or "work_beverage" => C01(_sim.ExpoLoad / 8f),
        "work_dt" or "work_counter" or "work_counter2" => C01(_sim.Tickets / 8f),
        _ => 0.3f,
    };

    static string StationLabel(string key) => key switch
    {
        "work_dt" => "Drive-Thru", "work_counter" => "Front Counter", "work_counter2" => "Front Counter 2",
        "work_grill" => "Grill", "work_fryer" => "Fryer", "work_assembly" => "Assembly", "work_expo" => "Expo",
        "work_beverage" => "Beverage", "work_prep" => "Prep", "work_office" => "Manager on Duty", _ => key
    };

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
        if (workKey == "work_counter" || workKey == "work_counter2") return new Vector3(home.X, 0, 0.6f);   // face the customer in the lobby
        return home + new Vector3(0, 0, -1f);
    }

    // Who is on the floor right now, from the roster: (station, role-string, id, name).
    // Membership only — break state is applied per-frame in SyncStaff.
    List<(string Station, string Role, int EmpId, string Name)> BuildStaffSpec(int minute)
    {
        var spec = new List<(string, string, int, string)>();
        foreach (var (sh, _) in _roster.PresentAt(RosterDay, minute))
        {
            string role = sh.Role switch
            {
                CrewRole.ShiftManager      => "shift_manager",
                CrewRole.AssistantManager  => "assistant_manager",
                CrewRole.RestaurantManager => "restaurant_manager",
                _                          => "crew",   // Crew + Lead render and behave as crew
            };
            spec.Add((sh.Station, role, sh.EmployeeId, sh.Name));
        }
        return spec;
    }

    bool StationBusy(string key) => key switch
    {
        "work_grill" => _sim.GrillLoad > 0,
        "work_fryer" => _sim.FryerLoad > 0,
        "work_assembly" => _sim.AssemblyLoad > 0,
        "work_beverage" or "work_expo" => _sim.ExpoLoad > 0,
        "work_prep" => _sim.Prep < 100,
        "work_counter" or "work_counter2" or "work_dt" => _sim.Tickets > 0,
        _ => false
    };
}
