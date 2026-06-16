using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RestaurantSimulator;

/// RS-IM-001 — real per-ingredient inventory, hold, and waste accounting.
///
/// Runs alongside the proven throughput engine (orders/abandonment/SOS are
/// unchanged): it is driven by the same deterministic item.taken stream, so the
/// same (catalog, scenario, seed) yields the same ledger. Each perishable
/// ingredient is tracked as dated lots that expire on THAT ingredient's own
/// hold_time at its own temperature — cooked patties on a 20-minute clock, fries
/// on 7, sliced tomato on 120, lettuce on 240, while buns, packaging and frozen
/// stock don't expire intra-day. Waste is recorded per ingredient and costed at
/// its unit price. This replaces the single 30-minute prep rule and two global
/// temperatures that produced the 60–77% phantom prep waste.
public sealed class IngredientLedger
{
    readonly IngredientCatalog _cat;
    public IngredientLedger(IngredientCatalog cat) { _cat = cat; }

    sealed class Stock
    {
        public double Opening, Received, Consumed, Waste;       // qty
        public double Balance;                                  // non-perishable on-hand
        public readonly List<(double Qty,double ReadyAt)> Lots = new(); // perishable, FIFO
        public double OnHand => Balance + LotSum();
        double LotSum(){ double s=0; foreach(var l in Lots) s+=l.Qty; return s; }
    }

    readonly Dictionary<string,Stock> _s = new();
    public bool Active => _cat is { Loaded: true };
    public double WasteCostUsd { get; private set; }
    public double WasteUnits { get; private set; }
    public int TempExcursions { get; private set; }

    Stock S(string id){ if(!_s.TryGetValue(id,out var st)){ st=new Stock(); _s[id]=st; } return st; }

    // ---- production/prep batch size by storage class (operator_calibration_required) ----
    static double Batch(IngredientCatalog.Ingredient i) => i.Storage switch
    {
        "hot_hold" => i.Id=="fries_cooked" ? 4 : 6,   // fries match the real fryer batch (4); 7-min hold punishes big batches   // cook-to-hold small batches turn over inside the hold window
        "prep_cold" => 40,                            // produce prepped a few hours ahead (long hold)
        _ => 0,                                        // non-perishable: bulk received on demand
    };

    /// Seed opening stock. Perishable items open EMPTY and are cooked/prepped
    /// just-in-time on first demand — so an ingredient the day never sells is never
    /// produced and never wastes (no phantom opening-batch expiry). Stable items
    /// open with a full day's bulk so they never gate service.
    public void OpenDay(double minute)
    {
        if(!Active) return;
        foreach(var i in _cat.Items.Values)
        {
            var st = S(i.Id);
            if(i.Perishable) continue;   // produced JIT in Draw(); opening = 0
            var q = i.Storage=="dry"||i.Storage=="ambient"||i.Storage=="frozen" ? 1200 : 600;
            st.Balance = q; st.Opening = q;
        }
    }

    /// Consume one menu item's bill of materials at the given minute.
    public void ConsumeMenuItem(string menuItem, double minute)
    {
        if(!Active) return;
        foreach(var (ingId, qty) in _cat.BomFor(menuItem)) Draw(ingId, qty, minute);
    }

    /// RS-IM-003 — order-level paper goods: one carry-out bag per order plus napkins
    /// scaled to order size. Tracked in inventory like any other consumable.
    public void ConsumeOrderPackaging(int itemCount, double minute)
    {
        if(!Active) return;
        Draw("carry_bag", 1, minute);
        Draw("napkin", Math.Max(2, itemCount * 2), minute);
    }

    /// Waste a non-perishable packaging item (e.g. a wrap or bag spoiled on a remake).
    public void WastePackaging(string id, double qty, double minute)
    {
        if(!Active) return;
        var ing = _cat.Get(id); if(ing==null || ing.Perishable) return;
        var st = S(id);
        if(st.Balance < qty - 1e-9){ var r = Math.Max(qty, 500); st.Balance += r; st.Received += r; }
        st.Balance -= qty; st.Waste += qty; WasteUnits += qty; WasteCostUsd += qty * ing.UnitCostUsd;
    }

    void Draw(string id, double qty, double minute)
    {
        var ing = _cat.Get(id); if(ing==null) return;
        var st = S(id);
        if(ing.Perishable)
        {
            var need = qty;
            // Just-in-time top-up: if the dated lots can't cover the draw, cook/prep
            // a fresh batch now (counts as received) — models cook-to-hold tied to
            // real demand, so only the unsold tail ever ages out.
            while(LotsAvailable(st) < need - 1e-9)
            {
                var b = Math.Max(need, Batch(ing));
                st.Lots.Add((b, minute)); st.Received += b;
                // Cooking draws down the raw source stock (e.g. frozen patty -> cooked patty).
                if(_cat.CookedFrom.TryGetValue(id, out var rawId)) Draw(rawId, b, minute);
            }
            // FIFO draw (oldest first)
            while(need > 1e-9 && st.Lots.Count>0)
            {
                var lot = st.Lots[0];
                var take = Math.Min(lot.Qty, need);
                lot.Qty -= take; need -= take; st.Consumed += take;
                if(lot.Qty <= 1e-9) st.Lots.RemoveAt(0); else st.Lots[0]=lot;
            }
        }
        else
        {
            if(st.Balance < qty - 1e-9)
            {
                var r = Math.Max(qty, 500);
                st.Balance += r; st.Received += r;
            }
            st.Balance -= qty; st.Consumed += qty;
        }
    }

    static double LotsAvailable(Stock st){ double s=0; foreach(var l in st.Lots) s+=l.Qty; return s; }

    /// Expire perishable lots past their own hold time; record per-item waste + cost.
    public void Tick(double minute)
    {
        if(!Active) return;
        foreach(var kv in _s)
        {
            var ing = _cat.Get(kv.Key); if(ing==null || !ing.Perishable) continue;
            var st = kv.Value; var hold = ing.HoldTimeMin!.Value;
            for(int j=st.Lots.Count-1;j>=0;j--)
                if(minute - st.Lots[j].ReadyAt > hold)
                {
                    var w = st.Lots[j].Qty; st.Lots.RemoveAt(j);
                    if(w>1e-9){ st.Waste+=w; WasteUnits+=w; WasteCostUsd+=w*ing.UnitCostUsd; }
                }
        }
    }

    /// Per-item temperature audit against each ingredient's own food-safe range.
    /// coolerF/hotHoldF/freezerF are the unit temperatures from the throughput sim.
    public void AuditTemps(double coolerF, double hotHoldF, double freezerF)
    {
        if(!Active) return;
        foreach(var i in _cat.Items.Values)
        {
            if(!i.TempMinF.HasValue && !i.TempMaxF.HasValue) continue;
            double t = i.Storage switch { "hot_hold"=>hotHoldF, "frozen"=>freezerF, _=>coolerF };
            if((i.TempMinF.HasValue && t < i.TempMinF.Value-1e-9) ||
               (i.TempMaxF.HasValue && t > i.TempMaxF.Value+1e-9)) TempExcursions++;
        }
    }

    static string Num(double v)=>v.ToString("0.##",CultureInfo.InvariantCulture);

    public string ToLedgerJson(string provenance)
    {
        var rows = new List<string>();
        double totalWasteCost=0;
        foreach(var i in _cat.Items.Values)
        {
            var st = S(i.Id);
            var closing = st.OnHand;
            var ok = Math.Abs(st.Opening + st.Received - st.Consumed - st.Waste - closing) < 0.01;
            var wasteCost = st.Waste * i.UnitCostUsd; totalWasteCost += wasteCost;
            rows.Add($"{{\"ingredient_id\":\"{i.Id}\",\"category\":\"{i.Category}\",\"storage\":\"{i.Storage}\",\"state\":\"{i.State}\","+
                     $"\"hold_time_min\":{(i.HoldTimeMin.HasValue?Num(i.HoldTimeMin.Value):"null")},"+
                     $"\"temp_min_f\":{(i.TempMinF.HasValue?Num(i.TempMinF.Value):"null")},\"temp_max_f\":{(i.TempMaxF.HasValue?Num(i.TempMaxF.Value):"null")},"+
                     $"\"unit\":\"{i.Unit}\",\"unit_cost_usd\":{Num(i.UnitCostUsd)},"+
                     $"\"opening\":{Num(st.Opening)},\"received_or_prepped\":{Num(st.Received)},\"consumed\":{Num(st.Consumed)},"+
                     $"\"waste\":{Num(st.Waste)},\"waste_cost_usd\":{Num(wasteCost)},\"closing\":{Num(closing)},\"reconciles\":{(ok?"true":"false")}}}");
        }
        return $"{{\"provenance\":{provenance},\"model\":\"per_ingredient_hold_and_waste\","+
               $"\"equation\":\"opening + received_or_prepped - consumed - waste = closing (per ingredient, on its own hold clock)\","+
               $"\"waste_cost_usd_total\":{Num(totalWasteCost)},\"waste_units_total\":{Num(WasteUnits)},\"temp_excursions\":{TempExcursions},"+
               $"\"ingredients\":[{string.Join(",",rows)}]}}";
    }

    /// Compact per-ingredient waste map for the end-of-shift summary.
    public string WasteByItemJson()
    {
        var parts = new List<string>();
        foreach(var i in _cat.Items.Values)
        {
            var st = S(i.Id);
            if(st.Waste>1e-9) parts.Add($"\"{i.Id}\":{{\"units\":{Num(st.Waste)},\"cost_usd\":{Num(st.Waste*i.UnitCostUsd)}}}");
        }
        return "{"+string.Join(",",parts)+"}";
    }
}
