using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace RestaurantSimulator;

/// Presentation-layer crowd authority. It owns visual destination slots,
/// reservations, and movement telemetry only; it never writes back to SimRunState.
public sealed class CrowdCoordinator
{
    sealed class Slot
    {
        public string Id = "";
        public string Type = "";
        public string Channel = "";
        public Vector3 Position;
        public float Radius;
        public int QueueIndex;
        public string? ReservedBy;
        public string? OccupiedBy;
    }

    readonly WorldLayout _world;
    readonly Dictionary<string, Slot> _slots = new();
    readonly List<Slot> _ordered = new();
    readonly string? _telemetryDir;
    readonly string? _samplePath;
    double _elapsed;
    double _nextSample;

    const double SampleCadenceSeconds = 0.5;

    public bool TelemetryEnabled => _samplePath != null;

    public CrowdCoordinator(WorldLayout world)
    {
        _world = world;
        BuildSlots();
        _telemetryDir = System.Environment.GetEnvironmentVariable("RS_MOVEMENT_TELEMETRY_DIR");
        if (!string.IsNullOrWhiteSpace(_telemetryDir))
        {
            Directory.CreateDirectory(_telemetryDir);
            _samplePath = Path.Combine(_telemetryDir, "movement_samples.jsonl");
            File.WriteAllText(_samplePath, "");
        }
    }

    public void Update(IReadOnlyList<CustomerAgent> customers, IReadOnlyList<EmployeeAgent> staff)
    {
        ClearReservations();
        AssignCustomers(customers);
        AssignEmployees(staff);
    }

    public void RecordTelemetry(double delta, IReadOnlyList<CustomerAgent> customers, IReadOnlyList<EmployeeAgent> staff, SimRunState sim)
    {
        if (_samplePath == null) return;
        _elapsed += delta;
        if (_elapsed + 0.0001 < _nextSample) return;
        _nextSample = _elapsed + SampleCadenceSeconds;

        var sb = new StringBuilder(8192);
        sb.Append("{\"type\":\"movement_sample\",\"time_sec\":").Append(Num(_elapsed))
          .Append(",\"sim_minute\":").Append(Num(sim.Minute)).Append(",\"agents\":[");

        bool first = true;
        foreach (var c in customers)
        {
            if (c == null || !GodotObject.IsInstanceValid(c)) continue;
            if (!first) sb.Append(',');
            first = false;
            AppendCustomer(sb, c);
        }
        foreach (var e in staff)
        {
            if (e == null || !GodotObject.IsInstanceValid(e)) continue;
            if (!first) sb.Append(',');
            first = false;
            AppendEmployee(sb, e);
        }

        sb.Append("],\"slots\":[");
        for (int i = 0; i < _ordered.Count; i++)
        {
            if (i > 0) sb.Append(',');
            var s = _ordered[i];
            sb.Append("{\"slot_id\":\"").Append(Json(s.Id)).Append("\",\"slot_type\":\"").Append(Json(s.Type))
              .Append("\",\"reserved_by\":").Append(JsonOrNull(s.ReservedBy))
              .Append(",\"occupied_by\":").Append(JsonOrNull(s.OccupiedBy)).Append('}');
        }
        sb.Append("],\"pairs\":[");
        AppendPairs(sb, customers, staff);
        sb.Append("]}");
        File.AppendAllText(_samplePath, sb.ToString() + "\n");
    }

    public Vector3 CustomerTarget(CustomerAgent customer, CustomerAgent.Phase phase)
    {
        return phase switch
        {
            CustomerAgent.Phase.Enter or CustomerAgent.Phase.Ordering => customer.QueueSpot,
            CustomerAgent.Phase.Waiting => customer.WaitSpot,
            CustomerAgent.Phase.ToPickup => customer.PickupSpot,
            CustomerAgent.Phase.Dining => customer.TableSpot,
            CustomerAgent.Phase.Busing => customer.BusSpot,
            CustomerAgent.Phase.Leave => customer.ExitSpot,
            _ => customer.Position,
        };
    }

    public Vector3 EmployeeTarget(EmployeeAgent employee, bool stationBusy)
    {
        if (employee.IsCashier && (stationBusy || employee.ShouldServeCustomer)) return employee.ServeSpot;
        return employee.HomeSpot;
    }

    public Vector3 EmployeeHome(EmployeeAgent employee) => employee.HomeSpot;

    void BuildSlots()
    {
        for (int i = 0; i < _world.QueueSpots.Count; i++)
            Add("counter_" + i, "CounterQueue", "lobby", _world.QueueSpots[i], 0.55f, i);
        for (int i = 0; i < 8; i++)
            Add("counter_overflow_" + i, "CounterQueue", "lobby", CounterOverflowSpot(i), 0.55f, _world.QueueSpots.Count + i);

        for (int i = 0; i < Math.Max(1, _world.KioskSpots.Count) * 4; i++)
            Add("kiosk_" + i, "KioskQueue", "lobby", KioskQueueSpot(i), 0.55f, i);
        Add("pos_order_0", "PosOrder", "lobby", CounterPosOrderSpot(0), 0.6f, 0);
        Add("pos_order_1", "PosOrder", "lobby", CounterPosOrderSpot(1), 0.6f, 1);
        Add("pos_service_0", "PosService", "staff", CounterPosServiceSpot(0), 0.65f, 0);
        Add("pos_service_1", "PosService", "staff", CounterPosServiceSpot(1), 0.65f, 1);

        for (int i = 0; i < 12; i++)
            Add("lobby_wait_" + i, "LobbyWait", "lobby", LobbyWaitSpot(i), 0.55f, i);
        for (int i = 0; i < 8; i++)
            Add("lobby_pickup_" + i, "LobbyPickup", "lobby", LobbyPickupSpot(i), 0.55f, i);
        for (int i = 0; i < 10; i++)
            Add("mobile_entry_" + i, "MobileEntry", "mobile", MobileEntrySpot(i), 0.55f, i);
        for (int i = 0; i < 10; i++)
            Add("mobile_wait_" + i, "MobileWait", "mobile", MobileWaitSpot(i), 0.55f, i);
        for (int i = 0; i < 10; i++)
            Add("mobile_pickup_" + i, "MobilePickup", "mobile", MobilePickupSpot(i), 0.55f, i);

        int diningIndex = 0;
        for (int i = 0; i < _world.Seats.Count; i++)
        {
            if (_world.Seats[i].EmployeeOnly) continue;
            Add("dining_" + diningIndex++, "Dining", "lobby", _world.Seats[i].Seat, 0.6f, i);
        }
        for (int i = 0; i < 6; i++)
            Add("tray_return_" + i, "TrayReturn", "lobby", _world.Anchor["pickup"] + new Vector3((i % 3 - 1) * 0.5f, 0f, 0.55f + (i / 3) * 0.45f), 0.55f, i);

        AddEmployeeSlots("work_grill", 3);
        AddEmployeeSlots("work_fryer", 3);
        AddEmployeeSlots("work_assembly", 4);
        AddEmployeeSlots("work_expo", 3);
        AddEmployeeSlots("work_prep", 2);
        AddEmployeeSlots("work_counter", 2);
        AddEmployeeSlots("work_counter2", 2);
        AddEmployeeSlots("work_dt", 2);
        AddEmployeeSlots("work_office", 3);
        var breakSeats = EmployeeBreakSeats();
        for (int i = 0; i < 6; i++)
            Add("break_" + i, "Break", "staff", breakSeats.Count > 0 ? breakSeats[i % breakSeats.Count].Seat : _world.Anchor["break_room"] + new Vector3((i % 3) * 0.65f - 0.65f, 0f, (i / 3) * 0.45f), 0.65f, i);
        for (int i = 0; i < 4; i++)
            Add("walkin_door_" + i, "WalkInDoor", "staff", _world.Anchor["freezer_door"] + new Vector3((i % 2) * 0.65f - 0.32f, 0f, 0.35f + (i / 2) * 0.45f), 0.65f, i);
        for (int i = 0; i < 6; i++)
            Add("walkin_standoff_" + i, "WalkInStandoff", "staff", _world.Anchor["freezer_door"] + new Vector3((i % 3 - 1) * 0.85f, 0f, 1.35f + (i / 3) * 0.65f), 0.65f, i);
    }

    void AddEmployeeSlots(string stationKey, int count)
    {
        for (int i = 0; i < count; i++)
            Add(stationKey + "_" + i, "EmployeeStation", "staff", EmployeeHomeSpot(stationKey, i), 0.65f, i);
    }

    void Add(string id, string type, string channel, Vector3 pos, float radius, int index)
    {
        var slot = new Slot { Id = id, Type = type, Channel = channel, Position = pos, Radius = radius, QueueIndex = index };
        _slots[id] = slot;
        _ordered.Add(slot);
    }

    void ClearReservations()
    {
        foreach (var s in _ordered)
        {
            s.ReservedBy = null;
            s.OccupiedBy = null;
        }
    }

    void AssignCustomers(IReadOnlyList<CustomerAgent> customers)
    {
        var kiosk = 0;
        var lobbyWait = 0;
        var lobbyPickup = 0;
        var mobileWait = 0;
        var mobilePickup = 0;
        var mobileEntry = 0;
        var dining = 0;
        var trayReturn = 0;
        var posOrder = 0;
        var counterLine = 0;

        for (int i = 0; i < customers.Count; i++)
        {
            var c = customers[i];
            if (c == null || !GodotObject.IsInstanceValid(c)) continue;
            c.SlotId = "";
            c.CanOrderNow = !c.IsCounterOrder;

            if (c.Channel == "lobby" && (c.State == CustomerAgent.Phase.Enter || c.State == CustomerAgent.Phase.Ordering))
            {
                if (c.UsesKiosk)
                    Reserve(c.AgentId, "kiosk_" + kiosk++, p => c.QueueSpot = p, c);
                else if (posOrder < 2)
                {
                    c.CanOrderNow = true;
                    Reserve(c.AgentId, "pos_order_" + posOrder++, p => c.QueueSpot = p, c);
                }
                else
                    Reserve(c.AgentId, "counter_" + counterLine++, p => c.QueueSpot = p, c);
            }
            else if (c.Channel == "lobby" && c.State == CustomerAgent.Phase.Waiting)
                Reserve(c.AgentId, "lobby_wait_" + lobbyWait++, p => c.WaitSpot = p, c);
            else if (c.Channel == "lobby" && c.State == CustomerAgent.Phase.ToPickup)
                Reserve(c.AgentId, "lobby_pickup_" + lobbyPickup++, p => c.PickupSpot = p, c);
            else if (c.Channel == "lobby" && c.State == CustomerAgent.Phase.Dining)
                ReserveDining(c, "dining_" + dining++);
            else if (c.Channel == "lobby" && c.State == CustomerAgent.Phase.Busing)
                Reserve(c.AgentId, "tray_return_" + trayReturn++, p => c.BusSpot = p, c);
            else if ((c.Channel == "mobile" || c.Channel == "delivery") && c.State == CustomerAgent.Phase.Enter)
                Reserve(c.AgentId, "mobile_entry_" + mobileEntry++, p => c.QueueSpot = p, c);
            else if ((c.Channel == "mobile" || c.Channel == "delivery") && c.State == CustomerAgent.Phase.Waiting)
                Reserve(c.AgentId, "mobile_wait_" + mobileWait++, p => c.WaitSpot = p, c);
            else if ((c.Channel == "mobile" || c.Channel == "delivery") && c.State == CustomerAgent.Phase.ToPickup)
                Reserve(c.AgentId, "mobile_pickup_" + mobilePickup++, p => c.PickupSpot = p, c);
        }
    }

    void AssignEmployees(IReadOnlyList<EmployeeAgent> staff)
    {
        var counts = new Dictionary<string, int>();
        int walkin = 0;
        int breaks = 0;
        int walkinStandoff = 0;
        for (int i = 0; i < staff.Count; i++)
        {
            var e = staff[i];
            if (e == null || !GodotObject.IsInstanceValid(e)) continue;
            e.SlotId = "";
            e.ActiveSlotId = "";
            e.ShouldServeCustomer = false;
            e.SupplyRunAllowed = false;
            int slot = counts.TryGetValue(e.StationKey, out var count) ? count : 0;
            counts[e.StationKey] = slot + 1;
            e.CrowdSlot = slot;

            e.HomeSpot = SlotPosition(e.StationKey + "_" + slot);
            e.CoolerSpot = SlotPosition("walkin_door_0");
            e.BreakSpot = SlotPosition("break_" + (breaks++ % 6));

            if (e.IsCashier)
                e.ServeSpot = e.StationKey == "work_dt"
                    ? _world.Anchor["dt_window"] + new Vector3(0.75f, 0f, 0f)
                    : e.HomeSpot + new Vector3(0f, 0f, 0.55f);

            if (e.OnBreak)
                Reserve(e.AgentId, "break_" + ((breaks - 1) % 6), p => e.BreakSpot = p, e);
            else if (e.WantsSupplyRun)
            {
                if (walkin == 0)
                {
                    e.SupplyRunAllowed = true;
                    Reserve(e.AgentId, "walkin_door_0", p => e.CoolerSpot = p, e);
                    walkin++;
                }
                else
                {
                    e.SupplyRunAllowed = false;
                    Reserve(e.AgentId, "walkin_standoff_" + walkinStandoff++, p => e.CoolerSpot = p, e);
                }
            }
            else if (e.IsCashier && TryCounterPosIndex(e.StationKey, out var posIdx) && PosNeedsService(posIdx))
            {
                e.ShouldServeCustomer = true;
                Reserve(e.AgentId, "pos_service_" + posIdx, p => e.ServeSpot = p, e);
            }
            else
                Reserve(e.AgentId, e.StationKey + "_" + slot, p => e.HomeSpot = p, e);
        }
    }

    bool PosNeedsService(int posIndex)
    {
        var id = "pos_order_" + posIndex;
        return _slots.TryGetValue(id, out var slot) && slot.ReservedBy != null;
    }

    static bool TryCounterPosIndex(string stationKey, out int posIndex)
    {
        if (stationKey == "work_counter") { posIndex = 0; return true; }
        if (stationKey == "work_counter2") { posIndex = 1; return true; }
        posIndex = -1;
        return false;
    }

    void Reserve(string agentId, string preferredSlotId, Action<Vector3> assign, CustomerAgent? customer)
    {
        var slot = SlotOrFallback(preferredSlotId);
        slot.ReservedBy = agentId;
        if (FlatDistance(customer?.Position ?? slot.Position, slot.Position) <= slot.Radius)
            slot.OccupiedBy = agentId;
        assign(slot.Position);
        if (customer != null) customer.SlotId = slot.Id;
    }

    void ReserveDining(CustomerAgent customer, string preferredSlotId)
    {
        var slot = SlotOrFallback(preferredSlotId);
        slot.ReservedBy = customer.AgentId;
        if (FlatDistance(customer.Position, slot.Position) <= slot.Radius)
            slot.OccupiedBy = customer.AgentId;
        customer.TableSpot = slot.Position;
        customer.SlotId = slot.Id;
        if (TrySeat(slot.QueueIndex, out var seat))
        {
            customer.TableSpot = seat.Seat;
            customer.TraySpot = seat.Tray;
            customer.SeatYawDeg = seat.YawDeg;
            customer.SeatTargetVisualYOffset = seat.VisualYOffset;
            customer.SeatKind = seat.Kind;
        }
    }

    void Reserve(string agentId, string preferredSlotId, Action<Vector3> assign, EmployeeAgent? employee)
    {
        var slot = SlotOrFallback(preferredSlotId);
        slot.ReservedBy = agentId;
        if (employee != null && FlatDistance(employee.Position, slot.Position) <= slot.Radius)
            slot.OccupiedBy = agentId;
        assign(slot.Position);
        if (employee != null) employee.SlotId = slot.Id;
    }

    Slot SlotOrFallback(string slotId)
    {
        if (_slots.TryGetValue(slotId, out var slot)) return slot;
        var prefix = slotId;
        int cut = slotId.LastIndexOf('_');
        if (cut > 0) prefix = slotId[..cut];
        for (int i = _ordered.Count - 1; i >= 0; i--)
            if (_ordered[i].Id.StartsWith(prefix, StringComparison.Ordinal)) return _ordered[i];
        return _ordered[0];
    }

    Vector3 SlotPosition(string slotId) => SlotOrFallback(slotId).Position;

    bool TrySeat(int index, out WorldLayout.SeatSpot seat)
    {
        if (index >= 0 && index < _world.Seats.Count)
        {
            seat = _world.Seats[index];
            return true;
        }
        seat = default!;
        return false;
    }

    List<WorldLayout.SeatSpot> EmployeeBreakSeats()
    {
        var seats = new List<WorldLayout.SeatSpot>();
        foreach (var s in _world.Seats)
            if (s.EmployeeOnly) seats.Add(s);
        return seats;
    }

    Vector3 CounterPosOrderSpot(int idx)
    {
        var key = idx == 0 ? "pos_register_1" : "pos_register_2";
        if (_world.Anchor.TryGetValue(key, out var p)) return new Vector3(p.X, 0f, 0.85f);
        return new Vector3(idx == 0 ? -2f : 2f, 0f, 0.85f);
    }

    Vector3 CounterPosServiceSpot(int idx)
    {
        var key = idx == 0 ? "work_counter" : "work_counter2";
        if (_world.Anchor.TryGetValue(key, out var p)) return p + new Vector3(0f, 0f, 0.55f);
        return new Vector3(idx == 0 ? -2f : 2f, 0f, -1.25f);
    }

    Vector3 CounterOverflowSpot(int idx)
    {
        if (_world.QueueSpots.Count == 0) return _world.Anchor["pickup"] + new Vector3(2.8f, 0f, 1.2f);
        return _world.QueueSpots[^1] + new Vector3((idx % 2) * 0.85f, 0f, 0.85f + (idx / 2) * 0.85f);
    }

    Vector3 KioskQueueSpot(int idx)
    {
        if (_world.KioskSpots.Count == 0) return CounterOverflowSpot(idx);
        int kioskCount = _world.KioskSpots.Count;
        return _world.KioskSpots[idx % kioskCount] + new Vector3(0f, 0f, (idx / kioskCount) * 0.78f);
    }

    Vector3 LobbyWaitSpot(int idx)
    {
        float lane = (idx % 3) - 1f;
        float row = idx / 3;
        return _world.Anchor["pickup"] + new Vector3(1.2f + lane * 0.95f, 0f, 1.75f + row * 0.95f);
    }

    Vector3 LobbyPickupSpot(int idx)
    {
        if (idx == 0) return _world.Anchor["pickup"] + new Vector3(0f, 0f, 0.35f);
        int n = idx - 1;
        return _world.Anchor["pickup"] + new Vector3((n % 3 - 1f) * 0.95f, 0f, 1.15f + (n / 3) * 0.95f);
    }

    Vector3 MobileWaitSpot(int idx)
    {
        float lane = (idx % 5) - 2f;
        return _world.Anchor["mobile_wait"] + new Vector3(lane * 0.55f, 0f, 0.9f + (idx / 5) * 0.72f);
    }

    Vector3 MobileEntrySpot(int idx)
    {
        float lane = (idx % 5) - 2f;
        return _world.Anchor["mobile_wait"] + new Vector3(lane * 0.55f, 0f, 0.15f + (idx / 5) * 0.72f);
    }

    Vector3 MobilePickupSpot(int idx)
    {
        float lane = (idx % 4) - 1.5f;
        return _world.Anchor["mobile_shelf"] + new Vector3(lane * 0.46f, 0f, 0.95f + (idx / 4) * 0.52f);
    }

    Vector3 EmployeeHomeSpot(string stationKey, int slot)
    {
        if (!_world.Anchor.TryGetValue(stationKey, out var baseSpot)) return Vector3.Zero;
        return stationKey switch
        {
            "work_assembly" => baseSpot + new Vector3((slot % 2 == 0 ? -0.9f : 0.9f), 0f, (slot / 2) * 0.75f),
            "work_grill" => baseSpot + new Vector3((slot % 3 - 1) * 0.7f, 0f, 0.1f + (slot / 3) * 0.55f),
            "work_fryer" => baseSpot + new Vector3((slot % 3 - 1) * 0.7f, 0f, 0.1f + (slot / 3) * 0.55f),
            "work_expo" => baseSpot + new Vector3((slot % 2) * 0.75f - 0.38f, 0f, (slot / 2) * 0.55f),
            "work_prep" => baseSpot + new Vector3((slot % 2) * 0.7f - 0.35f, 0f, (slot / 2) * 0.6f),
            "work_dt" => baseSpot + new Vector3(0f, 0f, (slot % 2) * 0.55f - 0.28f),
            "work_counter" or "work_counter2" => baseSpot + new Vector3((slot % 2) * 0.55f - 0.28f, 0f, 0f),
            "work_office" => baseSpot + new Vector3((slot % 2) * 0.55f - 0.28f, 0f, (slot / 2) * 0.45f),
            _ => baseSpot + new Vector3((slot % 3) * 0.55f - 0.55f, 0f, (slot / 3) * 0.45f),
        };
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.Y = 0f; b.Y = 0f;
        return (a - b).Length();
    }

    void AppendCustomer(StringBuilder sb, CustomerAgent c)
    {
        AppendAgentCommon(sb, c.AgentId, "customer", c.Channel, c.State.ToString(), c.Position, CustomerTarget(c, c.State), c.SlotId, c.MovementStuckSeconds, c.ApparentHeight);
        sb.Append(",\"ticket_id\":\"").Append(Json(c.OrderId)).Append("\",\"ticket_complete\":").Append(c.TicketDone ? "true" : "false")
          .Append(",\"carrying_food\":").Append(c.CarryingFood ? "true" : "false")
          .Append(",\"outside_store\":").Append(c.OutsideStore ? "true" : "false")
          .Append(",\"rotation_y_deg\":").Append(Num(Mathf.RadToDeg(c.Rotation.Y)))
          .Append(",\"seat_yaw_deg\":").Append(Num(c.SeatYawDeg))
          .Append(",\"seat_visual_y_offset\":").Append(Num(c.SeatTargetVisualYOffset))
          .Append(",\"seat_kind\":\"").Append(Json(c.SeatKind)).Append("\"")
          .Append(",\"phase_seconds\":").Append(Num(c.PhaseSeconds)).Append('}');
    }

    void AppendEmployee(StringBuilder sb, EmployeeAgent e)
    {
        var target = e.ActiveTarget == Vector3.Zero ? EmployeeTarget(e, e.ShouldServeCustomer) : e.ActiveTarget;
        AppendAgentCommon(sb, e.AgentId, "employee", "staff", e.Task, e.Position, target, e.SlotId, e.MovementStuckSeconds, e.ApparentHeight);
        sb.Append(",\"ticket_id\":null,\"ticket_complete\":false,\"carrying_food\":false,\"outside_store\":false,\"phase_seconds\":0}");
    }

    void AppendAgentCommon(StringBuilder sb, string id, string type, string channel, string phase, Vector3 pos, Vector3 target, string slotId, float stuck, float apparentHeight)
    {
        sb.Append("{\"agent_id\":\"").Append(Json(id)).Append("\",\"agent_type\":\"").Append(type)
          .Append("\",\"channel\":\"").Append(Json(channel)).Append("\",\"phase\":\"").Append(Json(phase))
          .Append("\",\"position\":").Append(Vec(pos)).Append(",\"target\":").Append(Vec(target))
          .Append(",\"slot_id\":\"").Append(Json(slotId)).Append("\",\"distance_to_target_m\":").Append(Num(FlatDistance(pos, target)))
          .Append(",\"stuck_seconds\":").Append(Num(stuck))
          .Append(",\"apparent_height_m\":").Append(Num(apparentHeight));
    }

    void AppendPairs(StringBuilder sb, IReadOnlyList<CustomerAgent> customers, IReadOnlyList<EmployeeAgent> staff)
    {
        var agents = new List<(string Id, Vector3 Pos, bool Exempt)>();
        foreach (var c in customers)
            if (c != null && GodotObject.IsInstanceValid(c)) agents.Add((c.AgentId, c.Position, false));
        foreach (var e in staff)
            if (e != null && GodotObject.IsInstanceValid(e)) agents.Add((e.AgentId, e.Position, e.IsRoaming));

        bool first = true;
        for (int i = 0; i < agents.Count; i++)
        {
            for (int j = i + 1; j < agents.Count; j++)
            {
                float d = FlatDistance(agents[i].Pos, agents[j].Pos);
                if (d >= 0.8f) continue;
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"a\":\"").Append(Json(agents[i].Id)).Append("\",\"b\":\"").Append(Json(agents[j].Id))
                  .Append("\",\"distance_m\":").Append(Num(d))
                  .Append(",\"exempt\":").Append((agents[i].Exempt || agents[j].Exempt) ? "true" : "false").Append('}');
            }
        }
    }

    static string Vec(Vector3 v) => "{\"x\":" + Num(v.X) + ",\"y\":" + Num(v.Y) + ",\"z\":" + Num(v.Z) + "}";
    static string Num(double n) => n.ToString("0.###", CultureInfo.InvariantCulture);
    static string JsonOrNull(string? s) => s == null ? "null" : "\"" + Json(s) + "\"";
    static string Json(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
