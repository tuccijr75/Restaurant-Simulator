using Godot;
using System.Collections.Generic;

namespace RestaurantSimulator;

/// Central reservation/slot coordinator for visual agents. It does not change
/// simulation state; it only assigns believable destinations so characters line
/// up, wait, and work from distinct positions instead of converging on one point.
public sealed class CrowdCoordinator
{
    readonly WorldLayout _world;

    public CrowdCoordinator(WorldLayout world)
    {
        _world = world;
    }

    public void UpdateCustomers(IReadOnlyList<CustomerAgent> customers)
    {
        int counter = 0, kiosk = 0, lobbyWait = 0, lobbyPickup = 0, mobileWait = 0, mobilePickup = 0;
        for (int i = 0; i < customers.Count; i++)
        {
            var c = customers[i];
            if (c == null || !GodotObject.IsInstanceValid(c)) continue;

            if (c.Channel == "lobby" && (c.State == CustomerAgent.Phase.Enter || c.State == CustomerAgent.Phase.Ordering))
            {
                c.QueueSpot = c.UsesKiosk ? KioskQueueSpot(kiosk++) : CounterQueueSpot(counter++);
            }

            if (c.Channel == "lobby" && c.State == CustomerAgent.Phase.Waiting)
                c.WaitSpot = LobbyWaitSpot(lobbyWait++);
            else if (c.Channel == "lobby" && c.State == CustomerAgent.Phase.ToPickup)
                c.PickupSpot = LobbyPickupSpot(lobbyPickup++);
            else if ((c.Channel == "mobile" || c.Channel == "delivery") && c.State == CustomerAgent.Phase.Waiting)
                c.WaitSpot = MobileWaitSpot(mobileWait++);
            else if ((c.Channel == "mobile" || c.Channel == "delivery") && c.State == CustomerAgent.Phase.ToPickup)
                c.PickupSpot = MobilePickupSpot(mobilePickup++);
        }
    }

    public void UpdateEmployees(IReadOnlyList<EmployeeAgent> staff)
    {
        var stationCounts = new Dictionary<string, int>();
        for (int i = 0; i < staff.Count; i++)
        {
            var e = staff[i];
            if (e == null || !GodotObject.IsInstanceValid(e)) continue;
            int slot = stationCounts.TryGetValue(e.StationKey, out var count) ? count : 0;
            stationCounts[e.StationKey] = slot + 1;

            e.CrowdSlot = slot;
            e.HomeSpot = EmployeeHomeSpot(e.StationKey, slot);
            if (e.IsCashier)
            {
                e.ServeSpot = e.StationKey == "work_dt"
                    ? _world.Anchor["dt_window"] + new Vector3(0.75f, 0f, 0f)
                    : e.HomeSpot + new Vector3(0f, 0f, 0.55f);
            }
            e.CoolerSpot = _world.Anchor["freezer_door"] + new Vector3((slot % 3) * 0.55f - 0.55f, 0f, 0.35f + (slot / 3) * 0.35f);
            e.BreakSpot = _world.Anchor["break_room"] + new Vector3((slot % 3) * 0.65f - 0.65f, 0f, (slot / 3) * 0.45f);
        }
    }

    public Vector3 CustomerTarget(CustomerAgent customer, CustomerAgent.Phase phase) => phase switch
    {
        CustomerAgent.Phase.Enter or CustomerAgent.Phase.Ordering => customer.QueueSpot,
        CustomerAgent.Phase.Waiting => customer.WaitSpot,
        CustomerAgent.Phase.ToPickup => customer.PickupSpot,
        CustomerAgent.Phase.Dining => customer.TableSpot,
        CustomerAgent.Phase.Busing => customer.BusSpot,
        CustomerAgent.Phase.Leave => customer.ExitSpot,
        _ => customer.Position,
    };

    public Vector3 EmployeeTarget(EmployeeAgent employee, bool stationBusy)
    {
        if (employee.IsCashier && stationBusy) return employee.ServeSpot;
        return employee.HomeSpot;
    }

    public Vector3 EmployeeHome(EmployeeAgent employee) => employee.HomeSpot;

    Vector3 CounterQueueSpot(int idx)
    {
        if (_world.QueueSpots.Count == 0) return _world.Anchor["pickup"] + new Vector3(2.8f, 0f, 1.2f);
        if (idx < _world.QueueSpots.Count) return _world.QueueSpots[idx];

        int overflow = idx - _world.QueueSpots.Count;
        return _world.QueueSpots[^1] + new Vector3((overflow % 2) * 0.85f, 0f, 0.85f + (overflow / 2) * 0.85f);
    }

    Vector3 KioskQueueSpot(int idx)
    {
        if (_world.KioskSpots.Count == 0) return CounterQueueSpot(idx);
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
}
