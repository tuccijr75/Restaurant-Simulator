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
        Box(w, new Vector3(14, 1.1f, 0.9f), new Vector3(-0.5f, 0.55f, -0.2f), Accent, "counter");
        Box(w, new Vector3(14, 0.06f, 1.1f), new Vector3(-0.5f, 1.13f, -0.2f), Steel, "counter_top");
        Station(w, L, "pos_register_1", new Vector3(-2.5f, 1.2f, -0.2f), new Vector3(0.4f, 0.35f, 0.3f), DarkSteel, "POS 1");
        Station(w, L, "pos_register_2", new Vector3(1.5f, 1.2f, -0.2f), new Vector3(0.4f, 0.35f, 0.3f), DarkSteel, "POS 2");
        Station(w, L, "mobile_shelf", new Vector3(5.5f, 1.0f, -0.1f), new Vector3(1.6f, 0.9f, 0.5f), Steel, "MOBILE PICKUP");
        // Menu boards above counter
        Box(w, new Vector3(7, 1.0f, 0.1f), new Vector3(-0.5f, 3.35f, -1.7f), new Color(0.12f, 0.12f, 0.14f), "menu_board");

        // ---------- kitchen stations ----------
        Station(w, L, "grill", new Vector3(-8.5f, 0.5f, -5.2f), new Vector3(2.6f, 1.0f, 1.4f), DarkSteel, "GRILL", glow: new Color(1f, 0.35f, 0.1f));
        Station(w, L, "fryer", new Vector3(-4.2f, 0.5f, -5.2f), new Vector3(2.4f, 1.0f, 1.4f), Steel, "FRYER BANK", glow: new Color(1f, 0.75f, 0.2f));
        Station(w, L, "prep", new Vector3(0.8f, 0.5f, -5.2f), new Vector3(2.8f, 0.95f, 1.4f), Steel, "PREP");
        Station(w, L, "cooler", new Vector3(-11.0f, 1.2f, -5.0f), new Vector3(1.6f, 2.4f, 2.6f), new Color(0.8f, 0.84f, 0.88f), "WALK-IN");
        Station(w, L, "assembly", new Vector3(-3.0f, 0.5f, -2.2f), new Vector3(4.4f, 0.95f, 1.1f), Steel, "ASSEMBLY");
        Station(w, L, "beverage", new Vector3(3.6f, 0.7f, -2.2f), new Vector3(2.2f, 1.5f, 1.0f), DarkSteel, "BEVERAGE");
        Station(w, L, "expo", new Vector3(0.5f, 0.8f, -1.1f), new Vector3(2.4f, 0.5f, 0.8f), Steel, "EXPO", glow: new Color(1f, 0.55f, 0.15f));
        Station(w, L, "office", new Vector3(9.8f, 0.5f, -5.2f), new Vector3(3.4f, 1.0f, 2.2f), new Color(0.55f, 0.45f, 0.35f), "OFFICE / BREAK");
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
        L.Anchor["pickup"] = new Vector3(1.5f, 0, 0.9f);
        L.Anchor["mobile_wait"] = new Vector3(5.5f, 0, 1.2f);
        L.Anchor["break_room"] = new Vector3(9.8f, 0, -3.6f);
        L.QueueSpots.Add(new Vector3(-2.5f, 0, 1.1f));
        L.QueueSpots.Add(new Vector3(-2.5f, 0, 2.2f));
        L.QueueSpots.Add(new Vector3(-2.5f, 0, 3.3f));
        L.QueueSpots.Add(new Vector3(-2.5f, 0, 4.4f));
        L.QueueSpots.Add(new Vector3(-2.0f, 0, 5.5f));
        // Employee work spots (sim station id -> world position, crew side of fixtures)
        L.Anchor["work_grill"] = new Vector3(-8.5f, 0, -4.0f);
        L.Anchor["work_fryer"] = new Vector3(-4.2f, 0, -4.0f);
        L.Anchor["work_prep"] = new Vector3(0.8f, 0, -4.0f);
        L.Anchor["work_assembly"] = new Vector3(-3.0f, 0, -3.4f);
        L.Anchor["work_beverage"] = new Vector3(3.6f, 0, -3.4f);
        L.Anchor["work_expo"] = new Vector3(0.5f, 0, -2.2f);
        L.Anchor["work_counter"] = new Vector3(-2.5f, 0, -1.0f);
        L.Anchor["work_dt"] = new Vector3(10.6f, 0, 0.6f);
        L.Anchor["work_office"] = new Vector3(9.8f, 0, -4.2f);
        L.Sun = _lastSun; L.SkyMat = _lastSky;
        L.LotLights.AddRange(_lotLights); _lotLights.Clear();
        // RS-VS-001: interior ceiling lights — dim by day, carry the room at night.
        foreach (var at in new[]{ new Vector3(-4f,3.9f,-3f), new Vector3(2f,3.9f,-3f), new Vector3(-2f,3.9f,3.5f), new Vector3(6f,3.9f,3.5f) })
        {
            var li = new OmniLight3D { Position = at, OmniRange = 9, LightEnergy = 0.25f, LightColor = new Color(1f,0.96f,0.9f) };
            w.AddChild(li);
            L.InteriorLights.Add(li);
        }
        return L;
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

    static void Station(Node3D w, WorldLayout L, string id, Vector3 pos, Vector3 size, Color color, string label, Color? glow = null)
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
