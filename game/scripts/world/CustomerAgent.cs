using Godot;

namespace RestaurantSimulator;

/// Walk-in guest (lobby / mobile / delivery driver). State machine:
/// enter -> queue/order (or pickup shelf) -> wait -> collect ->
///   dine-in:  sit + eat -> bus the tray to the counter -> leave
///   to-go:    carry the bag straight out.
public partial class CustomerAgent : CharacterRig
{
    public enum Phase { Enter, Ordering, Waiting, ToPickup, Dining, Busing, Leave, Done }
    public Phase State = Phase.Enter;
    public string OrderId = "";
    public string Channel = "lobby";
    public bool TicketDone;
    public Vector3 QueueSpot, PickupSpot, TableSpot, ExitSpot, BusSpot;
    public bool Courier;
    float _pause;
    float _dine;
    bool _carrying;
    bool _seatedAtTable;
    MeshInstance3D? _tray, _cup, _bag;

    // RS-VS-001: dine-in guests carry a tray (and must bus it); to-go carries a bag.
    // CarryHeight tunes where the tray rides on the model — raise/lower if it floats.
    public static float CarryHeight = 0.9f;

    public void Configure(float orderPause, float dineSeconds) { _pause = orderPause; _dine = dineSeconds; }

    // Dine-in only when this guest was given dine time and isn't a courier.
    bool DineIn => _dine > 0 && !Courier;

    void AttachCarry()
    {
        if (_carrying) return;
        _carrying = true;
        if (DineIn)
        {
            _tray = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.34f, 0.03f, 0.24f) },
                Position = new Vector3(0, CarryHeight, 0.30f),
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.55f, 0.3f, 0.15f) }
            };
            _cup = new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.045f, BottomRadius = 0.035f, Height = 0.12f },
                Position = new Vector3(0.08f, CarryHeight + 0.08f, 0.30f),
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.9f, 0.2f, 0.15f) }
            };
            AddChild(_tray); AddChild(_cup);
        }
        else
        {
            _bag = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(0.3f, 0.34f, 0.24f) },
                Position = new Vector3(0.30f, 0.55f, 0.12f),    // held low at the side
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.8f, 0.55f, 0.25f) }
            };
            AddChild(_bag);
        }
    }

    void TrayToTable()
    {
        if (_tray != null) _tray.Position = new Vector3(0, 0.78f, 0.34f);    // set down in front, table height
        if (_cup != null) _cup.Position = new Vector3(0.10f, 0.86f, 0.34f);
    }
    void TrayToHands()
    {
        if (_tray != null) _tray.Position = new Vector3(0, CarryHeight, 0.30f);
        if (_cup != null) _cup.Position = new Vector3(0.08f, CarryHeight + 0.08f, 0.30f);
    }
    void DropTray() { _tray?.QueueFree(); _cup?.QueueFree(); _tray = null; _cup = null; }

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
                    State = DineIn ? Phase.Dining : Phase.Leave;   // to-go leaves with the bag
                }
                break;
            case Phase.Dining:
                if (!_seatedAtTable)
                {
                    if (!StepToward(TableSpot, delta)) break;
                    _seatedAtTable = true;
                    RequestSeated(true);                 // actually sit at the table
                    TrayToTable();                       // tray goes down on the table
                    break;
                }
                if (!Seated) break;                      // settling onto the seat
                Moving = false;
                _dine -= delta;
                if (_dine <= 0) { RequestSeated(false); TrayToHands(); State = Phase.Busing; }
                break;
            case Phase.Busing:
                if (Seated) { RequestSeated(false); break; }                 // stand up first
                if (StepToward(BusSpot, delta)) { DropTray(); State = Phase.Leave; }   // return tray to the counter
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
