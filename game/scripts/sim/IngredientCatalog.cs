using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace RestaurantSimulator;

/// RS-IM-001 — real ingredient catalog. Each ingredient carries its own storage
/// state, hold window, food-safe temperature range, unit and cost (see
/// config/ingredients.json / docs/09). Loaded from res:// in Godot or the
/// filesystem in the harness; the JSON is embedded below so an absent file
/// reproduces identical runs (same pattern as SimConfig). The catalog is a
/// deterministic input — never a per-employee signal.
public sealed class IngredientCatalog
{
    public sealed class Ingredient
    {
        public string Id="", DisplayName="", Category="", Storage="", State="", Unit="";
        public double? HoldTimeMin;          // null = shelf-stable within a business day
        public double? TempMinF, TempMaxF;   // null = not temperature-controlled
        public double UnitCostUsd;
        public bool Perishable => HoldTimeMin.HasValue;   // has an intra-day expiry clock
    }

    public Dictionary<string,Ingredient> Items { get; } = new();
    public Dictionary<string,List<(string Ingredient,double Qty)>> Bom { get; } = new();
    public Dictionary<string,string> CookedFrom { get; } = new();   // cooked id -> raw source id
    public bool Loaded { get; private set; }

    public static IngredientCatalog Default()
    {
        var c = new IngredientCatalog();
        c.Load(Embedded);
        return c;
    }

    public void Load(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            Items.Clear(); Bom.Clear(); CookedFrom.Clear();
            foreach (var e in r.GetProperty("ingredients").EnumerateArray())
            {
                var ing = new Ingredient
                {
                    Id = Str(e,"id"), DisplayName = Str(e,"display_name"), Category = Str(e,"category"),
                    Storage = Str(e,"storage"), State = Str(e,"state"), Unit = Str(e,"unit"),
                    HoldTimeMin = NumN(e,"hold_time_min"), TempMinF = NumN(e,"temp_min_f"), TempMaxF = NumN(e,"temp_max_f"),
                    UnitCostUsd = Num(e,"unit_cost_usd"),
                };
                if (ing.Id.Length>0) Items[ing.Id] = ing;
                if (e.TryGetProperty("cook_to", out var ct) && ct.ValueKind==JsonValueKind.String)
                {
                    var cooked = ct.GetString();
                    if (!string.IsNullOrEmpty(cooked)) CookedFrom[cooked] = ing.Id;   // cooked <- raw
                }
            }
            if (r.TryGetProperty("menu_item_bill_of_materials", out var boms))
                foreach (var mi in boms.EnumerateObject())
                {
                    var lines = new List<(string,double)>();
                    foreach (var l in mi.Value.EnumerateArray())
                        lines.Add((l.GetProperty("ingredient").GetString()??"", l.GetProperty("qty").GetDouble()));
                    Bom[mi.Name] = lines;
                }
            Loaded = Items.Count>0;
        }
        catch { Loaded = false; }
    }

    public Ingredient Get(string id) => Items.TryGetValue(id, out var i) ? i : null;
    public IReadOnlyList<(string Ingredient,double Qty)> BomFor(string menuItem) =>
        Bom.TryGetValue(menuItem, out var b) ? b : Array.Empty<(string,double)>();

    static string Str(JsonElement e,string k){ return e.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.String ? v.GetString()! : ""; }
    static double Num(JsonElement e,string k){ return e.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.Number ? v.GetDouble() : 0; }
    static double? NumN(JsonElement e,string k){ return e.TryGetProperty(k,out var v)&&v.ValueKind==JsonValueKind.Number ? v.GetDouble() : (double?)null; }

    public const string Embedded = """
{
  "schema_version": "1.0.0",
  "generator_version": "game-0.4.0",
  "synthetic_data": true,
  "data_classification": "INTERNAL_SIM",
  "notes": "Real per-ingredient model (RS-IM-001). Each item carries its own storage state, hold window, food-safe temperature range, unit and cost. hold_time_min is the QUALITY/TPHC hold once the item reaches its held state (cooked, prepped, or opened); null = shelf-stable within a single business day. temp_min_f/temp_max_f are the food-safe holding range (FDA Food Code 2022): hot-held >=135F, cold-held <=41F, frozen <=0F, dry/ambient = null (not temperature-controlled). All values are operator_calibration_required synthetic defaults grounded in common QSR practice and the FDA Food Code; they are not a specific brand's standards.",
  "storage_states": ["frozen", "cooler", "prep_cold", "dry", "ambient", "hot_hold"],
  "ingredients": [
    {"id": "frozen_beef_patty",      "display_name": "Beef patty (frozen, 1/4 lb)", "category": "protein",  "storage": "frozen",    "state": "raw",     "hold_time_min": null, "temp_min_f": -10, "temp_max_f": 0,   "unit": "each",    "unit_cost_usd": 0.55, "cook_to": "beef_patty_cooked"},
    {"id": "frozen_chicken_fillet",  "display_name": "Chicken fillet (frozen)",     "category": "protein",  "storage": "frozen",    "state": "raw",     "hold_time_min": null, "temp_min_f": -10, "temp_max_f": 0,   "unit": "each",    "unit_cost_usd": 0.68, "cook_to": "grilled_chicken_cooked"},
    {"id": "frozen_breaded_chicken", "display_name": "Breaded chicken (frozen)",    "category": "protein",  "storage": "frozen",    "state": "raw",     "hold_time_min": null, "temp_min_f": -10, "temp_max_f": 0,   "unit": "each",    "unit_cost_usd": 0.64, "cook_to": "crispy_chicken_cooked"},
    {"id": "frozen_nuggets",         "display_name": "Chicken nuggets (frozen)",    "category": "protein",  "storage": "frozen",    "state": "raw",     "hold_time_min": null, "temp_min_f": -10, "temp_max_f": 0,   "unit": "each",    "unit_cost_usd": 0.075,"cook_to": "nugget_cooked"},
    {"id": "frozen_fries",           "display_name": "Fries (frozen)",              "category": "side",     "storage": "frozen",    "state": "raw",     "hold_time_min": null, "temp_min_f": -10, "temp_max_f": 0,   "unit": "serving", "unit_cost_usd": 0.16, "cook_to": "fries_cooked"},
    {"id": "frozen_egg_folded",      "display_name": "Folded egg (frozen)",         "category": "protein",  "storage": "frozen",    "state": "raw",     "hold_time_min": null, "temp_min_f": -10, "temp_max_f": 0,   "unit": "each",    "unit_cost_usd": 0.18, "cook_to": "egg_cooked"},
    {"id": "raw_sausage_patty",      "display_name": "Sausage patty (frozen)",      "category": "protein",  "storage": "frozen",    "state": "raw",     "hold_time_min": null, "temp_min_f": -10, "temp_max_f": 0,   "unit": "each",    "unit_cost_usd": 0.24, "cook_to": "sausage_cooked"},

    {"id": "beef_patty_cooked",      "display_name": "Beef patty (cooked)",         "category": "protein",  "storage": "hot_hold",  "state": "cooked",  "hold_time_min": 20,   "temp_min_f": 135, "temp_max_f": 165, "unit": "each",    "unit_cost_usd": 0.55, "pan": "grilled_main"},
    {"id": "grilled_chicken_cooked", "display_name": "Grilled chicken (cooked)",    "category": "protein",  "storage": "hot_hold",  "state": "cooked",  "hold_time_min": 20,   "temp_min_f": 135, "temp_max_f": 165, "unit": "each",    "unit_cost_usd": 0.68, "pan": "grilled_main"},
    {"id": "crispy_chicken_cooked",  "display_name": "Crispy chicken (fried)",      "category": "protein",  "storage": "hot_hold",  "state": "cooked",  "hold_time_min": 20,   "temp_min_f": 135, "temp_max_f": 175, "unit": "each",    "unit_cost_usd": 0.64, "pan": "fried_main"},
    {"id": "nugget_cooked",          "display_name": "Nuggets (fried)",             "category": "protein",  "storage": "hot_hold",  "state": "cooked",  "hold_time_min": 20,   "temp_min_f": 135, "temp_max_f": 175, "unit": "each",    "unit_cost_usd": 0.075,"pan": "fried_main"},
    {"id": "fries_cooked",           "display_name": "Fries (cooked)",              "category": "side",     "storage": "hot_hold",  "state": "cooked",  "hold_time_min": 7,    "temp_min_f": 135, "temp_max_f": 175, "unit": "serving", "unit_cost_usd": 0.16, "pan": "fries"},
    {"id": "egg_cooked",             "display_name": "Folded egg (cooked)",         "category": "protein",  "storage": "hot_hold",  "state": "cooked",  "hold_time_min": 20,   "temp_min_f": 135, "temp_max_f": 165, "unit": "each",    "unit_cost_usd": 0.18, "pan": "grilled_main"},
    {"id": "sausage_cooked",         "display_name": "Sausage patty (cooked)",      "category": "protein",  "storage": "hot_hold",  "state": "cooked",  "hold_time_min": 30,   "temp_min_f": 135, "temp_max_f": 165, "unit": "each",    "unit_cost_usd": 0.24, "pan": "grilled_main"},
    {"id": "coffee_brewed",          "display_name": "Brewed coffee",               "category": "beverage", "storage": "hot_hold",  "state": "ready",   "hold_time_min": 60,   "temp_min_f": 135, "temp_max_f": 185, "unit": "serving", "unit_cost_usd": 0.12, "pan": null},

    {"id": "shredded_lettuce",       "display_name": "Shredded lettuce",            "category": "produce",  "storage": "prep_cold", "state": "prepped", "hold_time_min": 240,  "temp_min_f": 33,  "temp_max_f": 41,  "unit": "portion", "unit_cost_usd": 0.06, "pan": null},
    {"id": "tomato_slice",           "display_name": "Tomato, sliced",              "category": "produce",  "storage": "prep_cold", "state": "prepped", "hold_time_min": 120,  "temp_min_f": 33,  "temp_max_f": 41,  "unit": "slice",   "unit_cost_usd": 0.10, "pan": null},
    {"id": "diced_onion",            "display_name": "Onion, diced",                "category": "produce",  "storage": "prep_cold", "state": "prepped", "hold_time_min": 360,  "temp_min_f": 33,  "temp_max_f": 41,  "unit": "portion", "unit_cost_usd": 0.03, "pan": null},
    {"id": "pickle_slices",          "display_name": "Pickle slices",               "category": "produce",  "storage": "cooler",    "state": "ready",   "hold_time_min": null, "temp_min_f": 33,  "temp_max_f": 41,  "unit": "portion", "unit_cost_usd": 0.02, "pan": null},
    {"id": "american_cheese_slice",  "display_name": "American cheese slice",       "category": "dairy",    "storage": "cooler",    "state": "ready",   "hold_time_min": null, "temp_min_f": 33,  "temp_max_f": 41,  "unit": "slice",   "unit_cost_usd": 0.08, "pan": null},
    {"id": "shredded_cheese",        "display_name": "Shredded cheese",             "category": "dairy",    "storage": "cooler",    "state": "ready",   "hold_time_min": null, "temp_min_f": 33,  "temp_max_f": 41,  "unit": "portion", "unit_cost_usd": 0.07, "pan": null},
    {"id": "milk",                   "display_name": "Milk",                        "category": "dairy",    "storage": "cooler",    "state": "ready",   "hold_time_min": null, "temp_min_f": 33,  "temp_max_f": 41,  "unit": "serving", "unit_cost_usd": 0.22, "pan": null},
    {"id": "mayo_portion",           "display_name": "Mayonnaise (opened)",         "category": "sauce",    "storage": "cooler",    "state": "ready",   "hold_time_min": null, "temp_min_f": 33,  "temp_max_f": 41,  "unit": "portion", "unit_cost_usd": 0.03, "pan": null},
    {"id": "special_sauce",          "display_name": "Special sauce (opened)",      "category": "sauce",    "storage": "cooler",    "state": "ready",   "hold_time_min": null, "temp_min_f": 33,  "temp_max_f": 41,  "unit": "portion", "unit_cost_usd": 0.04, "pan": null},
    {"id": "ketchup_portion",        "display_name": "Ketchup",                     "category": "sauce",    "storage": "ambient",   "state": "ready",   "hold_time_min": null, "temp_min_f": null,"temp_max_f": null,"unit": "portion", "unit_cost_usd": 0.01, "pan": null},

    {"id": "hamburger_bun",          "display_name": "Hamburger bun",               "category": "bread",    "storage": "dry",       "state": "ready",   "hold_time_min": null, "temp_min_f": null,"temp_max_f": null,"unit": "each",    "unit_cost_usd": 0.14, "pan": null},
    {"id": "toasted_bun",            "display_name": "Bun, toasted",                "category": "bread",    "storage": "ambient",   "state": "prepped", "hold_time_min": 30,   "temp_min_f": null,"temp_max_f": null,"unit": "each",    "unit_cost_usd": 0.14, "pan": null},
    {"id": "biscuit",                "display_name": "Biscuit (baked)",             "category": "bread",    "storage": "hot_hold",  "state": "cooked",  "hold_time_min": 30,   "temp_min_f": 135, "temp_max_f": 175, "unit": "each",    "unit_cost_usd": 0.16, "pan": null},

    {"id": "soda_syrup",             "display_name": "Soda syrup (BIB)",            "category": "beverage", "storage": "ambient",   "state": "ready",   "hold_time_min": null, "temp_min_f": null,"temp_max_f": null,"unit": "serving", "unit_cost_usd": 0.18, "pan": null},
    {"id": "ice",                    "display_name": "Ice",                         "category": "beverage", "storage": "frozen",    "state": "ready",   "hold_time_min": null, "temp_min_f": -10, "temp_max_f": 32,  "unit": "serving", "unit_cost_usd": 0.01, "pan": null},

    {"id": "drink_cup",              "display_name": "Drink cup + lid",             "category": "packaging","storage": "dry",       "state": "ready",   "hold_time_min": null, "temp_min_f": null,"temp_max_f": null,"unit": "each",    "unit_cost_usd": 0.05, "pan": null},
    {"id": "fry_carton",             "display_name": "Fry carton",                  "category": "packaging","storage": "dry",       "state": "ready",   "hold_time_min": null, "temp_min_f": null,"temp_max_f": null,"unit": "each",    "unit_cost_usd": 0.03, "pan": null},
    {"id": "sandwich_wrap",          "display_name": "Sandwich wrap/box",           "category": "packaging","storage": "dry",       "state": "ready",   "hold_time_min": null, "temp_min_f": null,"temp_max_f": null,"unit": "each",    "unit_cost_usd": 0.04, "pan": null},
    {"id": "carry_bag",              "display_name": "Carry-out bag",               "category": "packaging","storage": "dry",       "state": "ready",   "hold_time_min": null, "temp_min_f": null,"temp_max_f": null,"unit": "each",    "unit_cost_usd": 0.03, "pan": null},
    {"id": "napkin",                 "display_name": "Napkin",                      "category": "packaging","storage": "dry",       "state": "ready",   "hold_time_min": null, "temp_min_f": null,"temp_max_f": null,"unit": "each",    "unit_cost_usd": 0.004,"pan": null}
  ],
  "menu_item_bill_of_materials": {
    "classic_burger":   [{"ingredient": "beef_patty_cooked", "qty": 1}, {"ingredient": "hamburger_bun", "qty": 1}, {"ingredient": "american_cheese_slice", "qty": 1}, {"ingredient": "shredded_lettuce", "qty": 1}, {"ingredient": "tomato_slice", "qty": 1}, {"ingredient": "pickle_slices", "qty": 1}, {"ingredient": "diced_onion", "qty": 1}, {"ingredient": "ketchup_portion", "qty": 1}, {"ingredient": "sandwich_wrap", "qty": 1}],
    "deluxe_burger":    [{"ingredient": "beef_patty_cooked", "qty": 2}, {"ingredient": "hamburger_bun", "qty": 1}, {"ingredient": "american_cheese_slice", "qty": 2}, {"ingredient": "shredded_lettuce", "qty": 1}, {"ingredient": "tomato_slice", "qty": 2}, {"ingredient": "special_sauce", "qty": 1}, {"ingredient": "sandwich_wrap", "qty": 1}],
    "grilled_chicken_sandwich": [{"ingredient": "grilled_chicken_cooked", "qty": 1}, {"ingredient": "hamburger_bun", "qty": 1}, {"ingredient": "shredded_lettuce", "qty": 1}, {"ingredient": "tomato_slice", "qty": 1}, {"ingredient": "mayo_portion", "qty": 1}, {"ingredient": "sandwich_wrap", "qty": 1}],
    "crispy_chicken_sandwich":  [{"ingredient": "crispy_chicken_cooked", "qty": 1}, {"ingredient": "hamburger_bun", "qty": 1}, {"ingredient": "pickle_slices", "qty": 1}, {"ingredient": "mayo_portion", "qty": 1}, {"ingredient": "sandwich_wrap", "qty": 1}],
    "nuggets_6pc":      [{"ingredient": "nugget_cooked", "qty": 6}, {"ingredient": "fry_carton", "qty": 1}],
    "fries":            [{"ingredient": "fries_cooked", "qty": 1}, {"ingredient": "fry_carton", "qty": 1}],
    "fountain_drink":   [{"ingredient": "soda_syrup", "qty": 1}, {"ingredient": "ice", "qty": 1}, {"ingredient": "drink_cup", "qty": 1}],
    "breakfast_biscuit":[{"ingredient": "biscuit", "qty": 1}, {"ingredient": "sausage_cooked", "qty": 1}, {"ingredient": "egg_cooked", "qty": 1}, {"ingredient": "american_cheese_slice", "qty": 1}, {"ingredient": "sandwich_wrap", "qty": 1}]
  }
}
""";
}
