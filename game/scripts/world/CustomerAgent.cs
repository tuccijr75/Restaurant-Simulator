using Godot;
using System.Collections.Generic;

namespace RestaurantSimulator;

/// Walk-in guest (lobby / mobile / delivery driver). State machine:
/// enter -> queue/order (or pickup shelf) -> wait -> collect -> dine or leave.
public partial class CustomerAgent : CharacterRig
{
    public enum Phase { Enter, ToQueue, Ordering, Waiting, ToPickup, Dining, Leave, Done }
    public Phase State = Phase.Enter;
    public string OrderId = "";
    public string Channel = "lobby";
    public bool TicketDone;
    public Vector3 QueueSpot, PickupSpot, TableSpot, ExitSpot;
    float _pause;
    float _dine;
    bool _carrying;
    public bool Courier;

    // RS-VS-001: visible takeaway — tray + cup for guests, a bag for couriers.
    void AttachCarry()
    {
        if (_carrying) return;
        _carrying = true;
        if (Courier)
        {
            AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.3f, 0.34f, 0.24f) }, Position = new Vector3(0.32f, 0.62f, 0.1f),
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.8f, 0.55f, 0.25f) } });
            return;
        }
        AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.34f, 0.03f, 0.24f) }, Position = new Vector3(0, 1.02f, 0.26f),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.55f, 0.3f, 0.15f) } });
        AddChild(new MeshInstance3D { Mesh = new CylinderMesh { TopRadius = 0.045f, BottomRadius = 0.035f, Height = 0.12f }, Position = new Vector3(0.08f, 1.1f, 0.26f),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.9f, 0.2f, 0.15f) } });
    }

    public void Configure(float orderPause, float dineSeconds)
    {
        _pause = orderPause;
        _dine = dineSeconds;
    }

    /// Returns true when finished and safe to free.
    public bool Drive(float delta)
    {
        switch (State)
        {
            case Phase.Enter:
                if (StepToward(QueueSpot, delta)) State = Channel == "lobby" ? Phase.Ordering : Phase.Waiting;
                break;
            case Phase.Ordering:
                Working = false; Moving = false;
                _pause -= delta;
                if (_pause <= 0) State = Phase.Waiting;
                break;
            case Phase.Waiting:
                Moving = false;
                if (TicketDone) State = Phase.ToPickup;
                break;
            case Phase.ToPickup:
                if (StepToward(PickupSpot, delta))
                {
                    AttachCarry();
                    State = _dine > 0 ? Phase.Dining : Phase.Leave;
                }
                break;
            case Phase.Dining:
                if (!StepToward(TableSpot, delta)) break;
                _dine -= delta;
                if (_dine <= 0) State = Phase.Leave;
                break;
            case Phase.Leave:
                if (StepToward(ExitSpot, delta)) { State = Phase.Done; return true; }
                break;
            case Phase.Done:
                return true;
        }
        return false;
    }
}
