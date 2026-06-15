using Godot;
using System.Collections.Generic;

namespace RestaurantSimulator;

/// Procedurally builds the full restaurant interior and exterior — no imported
/// assets, so the scene runs from a clean clone. All positions are exposed as a
/// named anchor map that the camera system and character agents share.
public sealed class WorldLayout
{
    public Node3D RoofGroup = null!;   // toggled with R / auto-hidden for overhead & high free cam
    public DirectionalLight3D Sun = null!;            // RS-VS-001 day/night
    public ProceduralSkyMaterial SkyMat = null!;
    public readonly System.Collections.Generic.List<OmniLight3D> LotLights = new();
    public readonly System.Collections.Generic.List<OmniLight3D> InteriorLights = new();
    public readonly Dictionary<string, Vector3> Anchor = new();
    public readonly List<Vector3> DtLane = new();        // drive-thru waypoints
    public readonly List<Vector3> QueueSpots = new();    // lobby register queue
    public readonly List<Vector3> Tables = new();        // dining seats
    public NavigationRegion3D NavRegion = null!;         // baked walkable area
    public Vector3 Door => Anchor["door"];
}

public static class WorldBuilder
{
    // Palette
    static readonly Color FloorTile = new(0.82f, 0.80f, 0.76f);
    static readonly Color KitchenTile = new(0.62f, 0.64f, 0.66f);
    static readonly Color WallCol = new(0.93f, 0.90f, 0.84f);
    static readonly Color Accent = new(0.75f, 0.16f, 0.12f);
    static readonly Color Steel = new(0.65f, 0.68f, 0.72f);
    static readonly Color DarkSteel = new(0.35f, 0.37f, 0.40f);
    static readonly Color Asphalt = new(0.24f, 0.24f, 0.26f);
    static readonly Color Grass = new(0.30f, 0.46f, 0.24f);
    static readonly Color Glass = new(0.55f, 0.75f, 0.85f, 0.35f);

    public static WorldLayout Build(Node3D root)
    {
        var L = new WorldLayout();
        var w = new Node3D { Name = "World" };
        root.AddChild(w);

        Sky(w);

        // ---------- ground ----------
        Box(w, new Vector3(90, 0.1f, 90), new Vector3(0, -0.05f, 6), Grass, "grass");
        Box(w, new Vector3(46, 0.12f, 46), new Vector3(2, -0.04f, 6), Asphalt, "asphalt");
        Box(w, new Vector3(26, 0.14f, 2.2f), new Vector3(0, -0.02f, 8.2f), new Color(0.7f, 0.7f, 0.7f), "sidewalk");

        // ---------- building shell (24 x 14, walls h=4) ----------
        Box(w, new Vector3(24, 0.16f, 14), new Vector3(0, 0.0f, 0), FloorTile, "floor");
        Box(w, new Vector3(24, 0.02f, 6.6f), new Vector3(0, 0.10f, -3.7f), KitchenTile, "kitchen_floor");
        Box(w, new Vector3(24, 4, 0.3f), new Vector3(0, 2, -7), WallCol, "wall_back");
        Box(w, new Vector3(0.3f, 4, 14), new Vector3(-12, 2, 0), WallCol, "wall_west");
        // East wall with drive-thru window gap (z 0..1.4)
        Box(w, new Vector3(0.3f, 4, 6.6f), new Vector3(12, 2, -3.7f), WallCol, "wall_east_a");
        Box(w, new Vector3(0.3f, 4, 5.2f), new Vector3(12, 2, 4.4f), WallCol, "wall_east_b");
        Box(w, new Vector3(0.3f, 1.0f, 1.6f), new Vector3(12, 0.5f, 0.6f), WallCol, "wall_east_sill");
        Box(w, new Vector3(0.3f, 1.4f, 1.6f), new Vector3(12, 3.3f, 0.6f), WallCol, "wall_east_header");
        // Front wall: glass storefront with center door gap (x -1..1)
        Box(w, new Vector3(10.6f, 4, 0.3f), new Vector3(-6.7f, 2, 7), WallCol, "wall_front_w_frame");
        Box(w, new Vector3(10.6f, 4, 0.3f), new Vector3(6.7f, 2, 7), WallCol, "wall_front_e_frame");
        Glas(w, new Vector3(9.6f, 2.4f, 0.12f), new Vector3(-6.7f, 1.6f, 7.05f), "glass_w");
        Glas(w, new Vector3(9.6f, 2.4f, 0.12f), new Vector3(6.7f, 1.6f, 7.05f), "glass_e");
        Box(w, new Vector3(2.2f, 0.4f, 0.3f), new Vector3(0, 3.8f, 7), WallCol, "door_header");
        var roof = new Node3D { Name = "RoofGroup" };
        w.AddChild(roof);
        L.RoofGroup = roof;
        Box(roof, new Vector3(24.6f, 0.4f, 14.6f), new Vector3(0, 4.2f, 0), new Color(0.5f, 0.32f, 0.2f), "roof");
        Box(roof, new Vector3(25, 0.9f, 0.3f), new Vector3(0, 4.8f, 7.2f), Accent, "parapet_front");
        Box(roof, new Vector3(25, 0.9f, 0.3f), new Vector3(0, 4.8f, -7.2f), Accent, "parapet_back");
        Sign(roof, new Vector3(0, 5.0f, 7.45f), "QSR  No. 1024");

        // ---------- counter / front of house ----------
        var counterMi = Box(w, new Vector3(14, 1.1f, 0.9f), new Vector3(-0.5f, 0.55f, -0.2f), Accent, "counter");
        var counterTopMi = Box(w, new Vector3(14, 0.06f, 1.1f), new Vector3(-0.5f, 1.13f, -0.2f), Steel, "counter_top");
        if (LoadEquip(w, new[] { "counter" }, new Vector3(-0.5f, 0, -0.2f), "equip_counter", 180f) != null)
        { counterMi.Visible = false; counterTopMi.Visible = false; }   // 180° faces the service side into the kitchen; change to 0f if reversed
        Station(w, L, "pos_register_1", new Vector3(-2.5f, 1.2f, -0.2f), new Vector3(0.4f, 0.35f, 0.3f), DarkSteel, "POS 1", hide: true);
        Station(w, L, "pos_register_2", new Vector3(1.5f, 1.2f, -0.2f), new Vector3(0.4f, 0.35f, 0.3f), DarkSteel, "POS 2", hide: true);
        Station(w, L, "mobile_shelf", new Vector3(6.2f, 1.0f, 0.5f), new Vector3(1.0f, 1.4f, 0.45f), Steel, "MOBILE PICKUP");
        // Menu boards above counter
        Box(w, new Vector3(7, 1.0f, 0.1f), new Vector3(-0.5f, 3.35f, -1.7f), new Color(0.12f, 0.12f, 0.14f), "menu_board");

        // ---------- kitchen stations ----------
        Station(w, L, "grill", new Vector3(-7.2f, 0.5f, -5.2f), new Vector3(2.6f, 1.0f, 1.4f), DarkSteel, "GRILL", glow: new Color(1f, 0.35f, 0.1f));
        Station(w, L, "fryer", new Vector3(-4.2f, 0.5f, -5.2f), new Vector3(2.4f, 1.0f, 1.4f), Steel, "FRYER BANK", glow: new Color(1f, 0.75f, 0.2f));
        // fry holding unit — by the fryers; fryer fills it, expo pulls from it. Unmanned prop.
        Prop(w, "french_fries", new Vector3(-2.3f, 0.6f, -5.2f), new Vector3(1.0f, 1.2f, 0.7f), new Color(0.85f, 0.7f, 0.25f));
        Station(w, L, "prep", new Vector3(0.8f, 0.5f, -5.2f), new Vector3(2.8f, 0.95f, 1.4f), Steel, "PREP");
        Station(w, L, "cooler", new Vector3(-11.0f, 1.3f, -8.8f), new Vector3(3.0f, 2.6f, 3.2f), new Color(0.8f, 0.84f, 0.88f), "WALK-IN");
        // freezer door on the inside face of the back wall (crew access the outside unit through here)
        Box(w, new Vector3(1.6f, 2.2f, 0.16f), new Vector3(-11.0f, 1.2f, -6.78f), DarkSteel, "freezer_door");
        Station(w, L, "assembly", new Vector3(-3.0f, 0.5f, -2.2f), new Vector3(4.4f, 0.95f, 1.1f), Steel, "ASSEMBLY");
        Station(w, L, "beverage", new Vector3(3.6f, 0.7f, -2.2f), new Vector3(2.2f, 1.5f, 1.0f), DarkSteel, "BEVERAGE");
        Station(w, L, "expo", new Vector3(0.5f, 0.8f, -1.1f), new Vector3(2.4f, 0.5f, 0.8f), Steel, "EXPO", glow: new Color(1f, 0.55f, 0.15f));
        // hot-holding unit for cooked food (sandwiches, nuggets) — between assembly and expo. Unmanned prop.
        Prop(w, "holding_unit", new Vector3(2.4f, 0.7f, -2.8f), new Vector3(1.4f, 1.5f, 0.7f), new Color(0.86f, 0.7f, 0.4f));
        Station(w, L, "office", new Vector3(10.4f, 0.5f, -6.0f), new Vector3(2.4f, 1.0f, 1.6f), new Color(0.55f, 0.45f, 0.35f), "OFFICE");
        // break room (separate from the office): table + bench where crew sit on break
        Box(w, new Vector3(2.0f, 0.75f, 1.3f), new Vector3(7.2f, 0.40f, -6.1f), new Color(0.72f, 0.55f, 0.38f), "break_table");
        Box(w, new Vector3(2.4f, 0.45f, 0.4f), new Vector3(7.2f, 0.23f, -5.1f), Accent, "break_bench");
        Station(w, L, "dt_window", new Vector3(11.4f, 0.6f, 0.6f), new Vector3(0.8f, 1.1f, 1.4f), Steel, "DT WINDOW");

        // ---------- dining ----------
        foreach (var (x, z) in new[] { (-9f, 2.6f), (-9f, 5.2f), (-5.5f, 2.6f), (-5.5f, 5.2f), (8.6f, 2.6f), (8.6f, 5.2f) })
        {
            Box(w, new Vector3(1.3f, 0.08f, 1.3f), new Vector3(x, 0.78f, z), new Color(0.72f, 0.55f, 0.38f), "table");
            Box(w, new Vector3(0.16f, 0.74f, 0.16f), new Vector3(x, 0.37f, z), DarkSteel, "table_leg");
            Box(w, new Vector3(1.5f, 0.45f, 0.4f), new Vector3(x, 0.23f, z + 0.95f), Accent, "bench");
            L.Tables.Add(new Vector3(x, 0, z + 0.95f));
        }
        Box(w, new Vector3(0.6f, 1.0f, 0.6f), new Vector3(-11.2f, 0.5f, 6.2f), DarkSteel, "trash");

        // ---------- exterior: drive-thru lane ----------
        Box(w, new Vector3(3.4f, 0.13f, 36), new Vector3(14.6f, -0.02f, 0), new Color(0.30f, 0.30f, 0.33f), "dt_lane");
        for (int i = 0; i < 9; i++)
            Box(w, new Vector3(0.18f, 0.14f, 1.4f), new Vector3(16.4f, 0.0f, -15 + i * 4), new Color(0.9f, 0.8f, 0.2f), "lane_stripe");
        Station(w, L, "order_board", new Vector3(16.6f, 1.3f, -6.5f), new Vector3(0.3f, 2.0f, 1.8f), new Color(0.12f, 0.12f, 0.14f), "ORDER HERE");
        Box(w, new Vector3(3.6f, 0.18f, 3.2f), new Vector3(14.6f, 3.0f, -6.5f), Accent, "board_canopy");
        Box(w, new Vector3(0.18f, 3.0f, 0.18f), new Vector3(16.3f, 1.5f, -7.8f), DarkSteel, "canopy_post");
        L.DtLane.Add(new Vector3(14.6f, 0, -17));
        L.DtLane.Add(new Vector3(14.6f, 0, -11));
        L.DtLane.Add(new Vector3(14.6f, 0, -6.5f));   // order board stop
        L.DtLane.Add(new Vector3(14.6f, 0, -2.5f));
        L.DtLane.Add(new Vector3(14.6f, 0, 0.6f));    // window stop
        L.DtLane.Add(new Vector3(14.6f, 0, 8));
        L.DtLane.Add(new Vector3(14.6f, 0, 17));

        // ---------- exterior: parking ----------
        for (int i = 0; i < 8; i++)
        {
            float x = -10.5f + i * 3.0f;
            Box(w, new Vector3(0.14f, 0.13f, 5.0f), new Vector3(x, 0.0f, 13.5f), new Color(0.92f, 0.92f, 0.92f), "stripe");
        }
        L.Anchor["parking_row"] = new Vector3(-9, 0, 13.5f);
        Pole(w, new Vector3(20, 0, 18), "pole_sign", topSign: true);
        Pole(w, new Vector3(-13, 0, 10), "light_1");
        Pole(w, new Vector3(11, 0, 19), "light_2");
        foreach (var (x, z) in new[] { (-14f, 7.5f), (14f, 12f), (-8f, 20f), (5f, 21f) })
            Bush(w, new Vector3(x, 0, z));

        // ---------- shared anchors ----------
        L.Anchor["door"] = new Vector3(0, 0, 7.0f);
        L.Anchor["door_out"] = new Vector3(0, 0, 9.5f);
        L.Anchor["pickup"] = new Vector3(4.8f, 0, 0.9f);        // collect on the lobby side, away from the order registers
        L.Anchor["freezer_door"] = new Vector3(-10.3f, 0, -6.2f);  // inside, in front of the walk-in door
        L.Anchor["work_counter2"] = new Vector3(1.5f, 0, -1.0f);   // cashier behind POS 2
        L.Anchor["mobile_wait"] = new Vector3(6.2f, 0, 1.5f);   // in front of the mobile rack, lobby side
        L.Anchor["break_room"] = new Vector3(7.2f, 0, -4.5f);   // crew sit in front of the break bench
        L.QueueSpots.Add(new Vector3(-2.5f, 0, 1.1f));
        L.QueueSpots.Add(new Vector3(-2.5f, 0, 2.2f));
        L.QueueSpots.Add(new Vector3(-2.5f, 0, 3.3f));
        L.QueueSpots.Add(new Vector3(-2.5f, 0, 4.4f));
        L.QueueSpots.Add(new Vector3(-2.0f, 0, 5.5f));
        // Employee work spots (sim station id -> world position, crew side of fixtures)
        L.Anchor["work_grill"] = new Vector3(-7.2f, 0, -4.0f);
        L.Anchor["work_fryer"] = new Vector3(-4.2f, 0, -4.0f);
        L.Anchor["work_prep"] = new Vector3(0.8f, 0, -4.0f);
        L.Anchor["work_assembly"] = new Vector3(-3.0f, 0, -3.4f);
        L.Anchor["work_beverage"] = new Vector3(3.6f, 0, -3.4f);
        L.Anchor["work_expo"] = new Vector3(0.5f, 0, -2.2f);
        L.Anchor["work_counter"] = new Vector3(-2.5f, 0, -1.0f);
        L.Anchor["work_dt"] = new Vector3(10.2f, 0, 0.6f);   // pulled in so the worker is not in the window opening
        L.Anchor["work_office"] = new Vector3(10.4f, 0, -4.8f);  // manager stands in front of the desk
        L.Sun = _lastSun; L.SkyMat = _lastSky;
        L.LotLights.AddRange(_lotLights); _lotLights.Clear();
        // RS-VS-001: interior ceiling lights — dim by day, carry the room at night.
        foreach (var at in new[]{ new Vector3(-4f,3.9f,-3f), new Vector3(2f,3.9f,-3f), new Vector3(-2f,3.9f,3.5f), new Vector3(6f,3.9f,3.5f),
                                  new Vector3(10.4f,3.6f,-5.4f), new Vector3(7.2f,3.6f,-5.4f) })   // office + break room
        {
            var li = new OmniLight3D { Position = at, OmniRange = 9, LightEnergy = 0.25f, LightColor = new Color(1f,0.96f,0.9f) };
            w.AddChild(li);
            L.InteriorLights.Add(li);
        }
        BuildExtras(w, L);
        BuildNavigation(w, L);
        return L;
    }

    // Tunables — raise AgentRadius to keep agents further off equipment; lower CellSize for finer paths.
    public static float NavAgentRadius = 0.35f;
    public static float NavCellSize = 0.12f;

    // Bake a walkable navigation mesh from the floor, carving around equipment, counters and walls,
    // so agents path *around* fixtures instead of straight through them.
    static void BuildNavigation(Node3D w, WorldLayout L)
    {
        var src = new NavigationMeshSourceGeometryData3D();
        int walk = 0, obs = 0;
        foreach (var ch in w.GetChildren())
        {
            if (ch is not MeshInstance3D mi || mi.Mesh == null) continue;
            string n = mi.Name;
            bool walkable = n == "floor" || n == "kitchen_floor" || n == "sidewalk";
            bool obstacle = n.StartsWith("st_") || n.StartsWith("counter") || n.StartsWith("wall_")
                            || n == "trash" || n == "break_table";
            if (walkable) { src.AddMesh(mi.Mesh, mi.Transform); walk++; }
            else if (obstacle) { src.AddMesh(mi.Mesh, mi.Transform); obs++; }
        }
        var nm = new NavigationMesh
        {
            CellSize = NavCellSize,
            CellHeight = 0.1f,
            AgentRadius = NavAgentRadius,
            AgentHeight = 1.6f,
            AgentMaxClimb = 0.3f,
            AgentMaxSlope = 45f,
        };
        NavigationServer3D.BakeFromSourceGeometryData(nm, src);
        var region = new NavigationRegion3D { Name = "NavRegion", NavigationMesh = nm };
        w.AddChild(region);
        L.NavRegion = region;
        GD.Print($"[Nav] baked navmesh: walkable={walk} obstacles={obs} polys={nm.GetPolygonCount()}");
    }

    // ---------- helpers ----------
    static void Sky(Node3D w)
    {
        var sky = new ProceduralSkyMaterial
        {
            SkyTopColor = new Color(0.35f, 0.55f, 0.85f),
            SkyHorizonColor = new Color(0.75f, 0.82f, 0.9f),
            GroundBottomColor = new Color(0.2f, 0.25f, 0.2f),
            GroundHorizonColor = new Color(0.6f, 0.65f, 0.6f)
        };
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = sky },
            AmbientLightSource = Godot.Environment.AmbientSource.Sky,
            AmbientLightEnergy = 1.1f
        };
        w.AddChild(new WorldEnvironment { Environment = env, Name = "Env" });
        var sun = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-52, -35, 0),
            LightEnergy = 1.15f,
            ShadowEnabled = true,
            Name = "Sun"
        };
        w.AddChild(sun);
        _lastSun = sun; _lastSky = sky;
    }
    static readonly System.Collections.Generic.List<OmniLight3D> _lotLights = new();
    static DirectionalLight3D _lastSun = null!;
    static ProceduralSkyMaterial _lastSky = null!;

    public static MeshInstance3D Box(Node3D parent, Vector3 size, Vector3 pos, Color color, string name)
    {
        var mi = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size },
            Position = pos,
            Name = name,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = color, Roughness = 0.85f }
        };
        parent.AddChild(mi);
        return mi;
    }

    static void Glas(Node3D parent, Vector3 size, Vector3 pos, string name)
    {
        var mi = Box(parent, size, pos, Glass, name);
        ((StandardMaterial3D)mi.MaterialOverride).Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
    }

    static void Station(Node3D w, WorldLayout L, string id, Vector3 pos, Vector3 size, Color color, string label, Color? glow = null, bool hide = false)
    {
        var mi = Box(w, size, pos, color, "st_" + id);
        // RS-GP-001: clickable — a static body with the station id in metadata.
        var body = new StaticBody3D { Position = pos, Name = "click_" + id };
        var shape = new CollisionShape3D { Shape = new BoxShape3D { Size = size } };
        body.AddChild(shape);
        body.SetMeta("station", id);
        w.AddChild(body);
        if (glow.HasValue)
        {
            var m = (StandardMaterial3D)mi.MaterialOverride;
            m.EmissionEnabled = true;
            m.Emission = glow.Value;
            m.EmissionEnergyMultiplier = 0.35f;
        }
        var tag = new Label3D
        {
            Text = label,
            FontSize = 56,
            Position = pos + new Vector3(0, size.Y / 2 + 0.55f, 0),
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Modulate = new Color(1, 1, 1),
            OutlineSize = 10,
            Name = "lbl_" + id
        };
        w.AddChild(tag);
        L.Anchor[id] = pos;
        if (hide) mi.Visible = false;   // e.g. POS: the counter's monitor mesh is the visual; box stays for click/anchor
        var cands = EquipAlias.TryGetValue(id, out var alias)
            ? new[] { "st_" + id, id, alias }
            : new[] { "st_" + id, id };
        if (LoadEquip(w, cands, pos, "equip_" + id) != null) mi.Visible = false;
    }

    // An unmanned equipment piece: a navmesh obstacle that loads st_<id>.glb if present.
    // No work anchor, click target, or label (it's a prop in the workflow, not a station).
    static void Prop(Node3D w, string id, Vector3 pos, Vector3 size, Color color, string? model = null)
    {
        var mi = Box(w, size, pos, color, "st_" + id);
        var cands = model != null ? new[] { model } : new[] { "st_" + id, id };
        if (LoadEquip(w, cands, pos, "equip_" + id) != null) mi.Visible = false;
    }

    // Beverage machines, self-serve fountains, kiosks, and the enclosed office + restroom rooms.
    // NOTE: positions here are rough placeholders pending the full floor-plan remodel.
    static void BuildExtras(Node3D w, WorldLayout L)
    {
        var steel = new Color(0.82f, 0.84f, 0.88f);
        // crew beverage by the drive-thru: soda + shake (shake on the open/aisle side, reachable by all)
        Prop(w, "soda_dt", new Vector3(8.9f, 0.7f, -1.0f), new Vector3(1.0f, 1.5f, 0.7f), steel, model: "soda_machine");
        Prop(w, "shake_dt", new Vector3(7.7f, 0.7f, -1.0f), new Vector3(0.9f, 1.5f, 0.7f), steel, model: "shake_machine");
        // self-serve soda fountains in the lobby
        Prop(w, "soda_lobby_1", new Vector3(-6.0f, 0.7f, 6.1f), new Vector3(1.0f, 1.5f, 0.7f), steel, model: "soda_machine");
        Prop(w, "soda_lobby_2", new Vector3(-3.0f, 0.7f, 6.1f), new Vector3(1.0f, 1.5f, 0.7f), steel, model: "soda_machine");
        // two self-order kiosks, center lobby
        Prop(w, "kiosk_1", new Vector3(3.2f, 0.6f, 3.4f), new Vector3(0.7f, 1.4f, 0.5f), new Color(0.2f, 0.22f, 0.26f), model: "kiosk");
        Prop(w, "kiosk_2", new Vector3(5.0f, 0.6f, 3.4f), new Vector3(0.7f, 1.4f, 0.5f), new Color(0.2f, 0.22f, 0.26f), model: "kiosk");

        // --- Office room (back-right corner; back wall z=-7 and east wall x=12 already exist) ---
        // West wall (faces kitchen): a window in back, a door to the kitchen in front.
        WallAlongZ(w, 8.0f, -7.0f, -5.0f, "wall_office_w1", openAt: -6.0f, openW: 1.2f, window: true);
        WallAlongZ(w, 8.0f, -5.0f, -3.6f, "wall_office_w2", openAt: -4.3f, openW: 1.0f);
        // South wall (faces lobby): a large window so the manager can see the floor.
        WallAlongX(w, -3.6f, 8.0f, 12.0f, "wall_office_s", openAt: 10.0f, openW: 2.2f, window: true);

        // --- Restroom room (front-left corner; west wall x=-12 and front wall z=7 already exist) ---
        WallAlongZ(w, -8.5f, 4.0f, 7.0f, "wall_bath_e", openAt: 5.6f, openW: 1.0f);   // door to lobby
        WallAlongX(w, 4.0f, -12.0f, -8.5f, "wall_bath_s", openAt: 0f, openW: 0f);
        Label(w, "RESTROOMS", new Vector3(-10.2f, 2.6f, 4.1f));
    }

    static void Label(Node3D w, string text, Vector3 pos)
    {
        w.AddChild(new Label3D
        {
            Text = text, FontSize = 56, Position = pos,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Modulate = new Color(1, 1, 1), OutlineSize = 10, Name = "lbl_room"
        });
    }

    const float WallH = 4f;
    // Wall running along X at fixed z; optional centered opening (door = full-height gap, window = sill+header).
    static void WallAlongX(Node3D w, float z, float x0, float x1, string name, float openAt = 0, float openW = 0, bool window = false, float thick = 0.2f)
    {
        if (openW <= 0f) { Box(w, new Vector3(x1 - x0, WallH, thick), new Vector3((x0 + x1) / 2, WallH / 2, z), WallCol, name); return; }
        float a = openAt - openW / 2, b = openAt + openW / 2;
        if (a > x0) Box(w, new Vector3(a - x0, WallH, thick), new Vector3((x0 + a) / 2, WallH / 2, z), WallCol, name + "_a");
        if (x1 > b) Box(w, new Vector3(x1 - b, WallH, thick), new Vector3((b + x1) / 2, WallH / 2, z), WallCol, name + "_b");
        if (window)
        {
            Box(w, new Vector3(openW, 0.9f, thick), new Vector3(openAt, 0.45f, z), WallCol, name + "_sill");
            Box(w, new Vector3(openW, 0.7f, thick), new Vector3(openAt, 3.65f, z), WallCol, name + "_hdr");
        }
    }
    // Wall running along Z at fixed x; optional centered opening.
    static void WallAlongZ(Node3D w, float x, float z0, float z1, string name, float openAt = 0, float openW = 0, bool window = false, float thick = 0.2f)
    {
        if (openW <= 0f) { Box(w, new Vector3(thick, WallH, z1 - z0), new Vector3(x, WallH / 2, (z0 + z1) / 2), WallCol, name); return; }
        float a = openAt - openW / 2, b = openAt + openW / 2;
        if (a > z0) Box(w, new Vector3(thick, WallH, a - z0), new Vector3(x, WallH / 2, (z0 + a) / 2), WallCol, name + "_a");
        if (z1 > b) Box(w, new Vector3(thick, WallH, z1 - b), new Vector3(x, WallH / 2, (b + z1) / 2), WallCol, name + "_b");
        if (window)
        {
            Box(w, new Vector3(thick, 0.9f, openW), new Vector3(x, 0.45f, openAt), WallCol, name + "_sill");
            Box(w, new Vector3(thick, 0.7f, openW), new Vector3(x, 3.65f, openAt), WallCol, name + "_hdr");
        }
    }

    // Equipment files whose name doesn't match the station id.
    static readonly System.Collections.Generic.Dictionary<string, string> EquipAlias = new()
    {
        ["dt_window"] = "drive_thru",
        ["cooler"] = "walk_in",
        ["office"] = "desk",
        ["mobile_shelf"] = "mobile_order",
    };

    static readonly string EquipDir = "res://models/kitchen/";
    const float FloorTop = 0.1f;

    // Try each candidate filename in res://models/kitchen/; on the first hit, instance
    // it, sit its base on the floor (any origin), name it equip_* (navmesh ignores it),
    // and return it so the caller can hide the placeholder box(es).
    static Node3D? LoadEquip(Node3D w, string[] candidates, Vector3 pos, string nodeName, float yawDeg = 0f)
    {
        foreach (var cand in candidates)
        {
            string path = EquipDir + cand + ".glb";
            if (!ResourceLoader.Exists(path)) continue;
            var packed = GD.Load<PackedScene>(path);
            if (packed == null) continue;
            var inst = packed.Instantiate<Node3D>();
            inst.Name = nodeName;
            if (yawDeg != 0f) inst.RotationDegrees = new Vector3(0, yawDeg, 0);
            w.AddChild(inst);
            inst.Position = new Vector3(pos.X, 0, pos.Z);
            inst.Position += new Vector3(0, FloorTop - WorldMinY(inst), 0);  // base -> floor
            GD.Print($"[Equip] {nodeName}: loaded {cand}.glb");
            return inst;
        }
        return null;
    }

    static float WorldMinY(Node3D root)
    {
        float min = float.PositiveInfinity;
        var stack = new System.Collections.Generic.Stack<Node>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var n = stack.Pop();
            if (n is MeshInstance3D mi && mi.Mesh != null)
            {
                var aabb = mi.GetAabb();
                var gt = mi.GlobalTransform;
                for (int c = 0; c < 8; c++)
                {
                    var corner = aabb.Position + new Vector3(
                        (c & 1) != 0 ? aabb.Size.X : 0,
                        (c & 2) != 0 ? aabb.Size.Y : 0,
                        (c & 4) != 0 ? aabb.Size.Z : 0);
                    float y = (gt * corner).Y;
                    if (y < min) min = y;
                }
            }
            foreach (var ch in n.GetChildren()) stack.Push(ch);
        }
        return float.IsInfinity(min) ? 0f : min;
    }

    static void Pole(Node3D w, Vector3 at, string name, bool topSign = false)
    {
        Box(w, new Vector3(0.25f, 7f, 0.25f), at + new Vector3(0, 3.5f, 0), DarkSteel, name);
        if (topSign)
        {
            Box(w, new Vector3(3.4f, 1.6f, 0.3f), at + new Vector3(0, 7.6f, 0), Accent, name + "_sign");
            var t = new Label3D
            {
                Text = "QSR", FontSize = 160,
                Position = at + new Vector3(0, 7.6f, 0.2f),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                OutlineSize = 14
            };
            w.AddChild(t);
        }
        else
        {
            var lamp = new OmniLight3D { Position = at + new Vector3(0, 6.8f, 0), OmniRange = 12, LightEnergy = 0.6f };
            w.AddChild(lamp);
            _lotLights.Add(lamp);
        }
    }

    static void Bush(Node3D w, Vector3 at)
    {
        var mi = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.7f, Height = 1.1f },
            Position = at + new Vector3(0, 0.5f, 0),
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.18f, 0.4f, 0.16f), Roughness = 1f }
        };
        w.AddChild(mi);
    }

    static void Sign(Node3D w, Vector3 at, string text)
    {
        var t = new Label3D
        {
            Text = text, FontSize = 140, Position = at,
            Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
            OutlineSize = 16, Modulate = new Color(1f, 0.95f, 0.9f)
        };
        w.AddChild(t);
    }
}
