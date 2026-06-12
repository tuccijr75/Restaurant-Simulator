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
            case "lobby": SpawnWalkin(orderId, channel, fromDoor: true); break;
            case "mobile": SpawnWalkin(orderId, channel, fromDoor: true); break;
            case "delivery": SpawnWalkin(orderId, channel, fromDoor: true, courier: true); break;
        }
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
        a.Position = _world.Anchor["door_out"] + new Vector3((float)(_vis.NextDouble() * 2 - 1), 0, 0.5f);
        int q = System.Math.Min(CountQueued(), _world.QueueSpots.Count - 1);
        a.QueueSpot = channel == "lobby"
            ? _world.QueueSpots[q]
            : _world.Anchor["mobile_wait"] + new Vector3((float)(_vis.NextDouble() * 1.6f - 0.8f), 0, 0.3f);
        a.PickupSpot = channel == "lobby" ? _world.Anchor["pickup"] : _world.Anchor["mobile_shelf"] + new Vector3(0, 0, 1.0f);
        bool dines = channel == "lobby" && !courier && _vis.NextDouble() < 0.35 && _world.Tables.Count > 0;
        a.TableSpot = dines ? _world.Tables[_vis.Next(_world.Tables.Count)] : Vector3.Zero;
        a.ExitSpot = _world.Anchor["door_out"] + new Vector3(0, 0, 2.5f);
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
        string sig = string.Join("|", spec);
        if (sig != _staffSignature)
        {
            _staffSignature = sig;
            foreach (var e in _staff) e.QueueFree();
            _staff.Clear();
            int i = 0;
            foreach (var key in spec)
            {
                var e = new EmployeeAgent { StationKey = key };
                bool mgr = key == "work_office";
                bool lead = key == "work_expo" && i == 0;
                var shirt = mgr ? new Color(0.92f, 0.92f, 0.92f) : lead ? new Color(0.1f, 0.1f, 0.12f) : new Color(0.75f, 0.16f, 0.12f);
                e.BuildHuman(shirt, new Color(0.12f, 0.12f, 0.15f), Skins[i % Skins.Length], new Color(0.75f, 0.16f, 0.12f));
                e.HomeSpot = _world.Anchor[key] + new Vector3((i % 3) * 0.55f - 0.55f, 0, 0);
                e.CoolerSpot = _world.Anchor["cooler"] + new Vector3(1.3f, 0, 0.4f);
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
            e.OnBreak = k < onBreak && e.StationKey != "work_office";
            e.Drive(d, StationBusy(e.StationKey));
        }
    }

    List<string> BuildStaffSpec()
    {
        var spec = new List<string>();
        string[] kitchenCycle = { "work_grill", "work_assembly", "work_expo" };
        for (int i = 0; i < _sim.KitchenCoverage; i++) spec.Add(kitchenCycle[i % kitchenCycle.Length]);
        for (int i = 0; i < _sim.FryerCoverage; i++) spec.Add("work_fryer");
        string[] driveCycle = { "work_dt", "work_beverage" };
        for (int i = 0; i < _sim.DriveCoverage; i++) spec.Add(driveCycle[i % driveCycle.Length]);
        for (int i = 0; i < _sim.CounterCoverage; i++) spec.Add("work_counter");
        for (int i = 0; i < _sim.PrepCoverage; i++) spec.Add("work_prep");
        int managers = _sim.ShiftMgr + _sim.AsstMgr + _sim.RestMgr;
        for (int i = 0; i < managers; i++) spec.Add("work_office");
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
