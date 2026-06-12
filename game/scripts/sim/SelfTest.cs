using System;
using System.Collections.Generic;
using System.Text;

namespace RestaurantSimulator;

/// In-engine release gate (F9): runs the same (scenario, seed) twice headless and
/// proves deterministic replay, lifecycle ordering, ledger reconciliation, and the
/// absence of deprecated item.sold — the control-pack diagnostics doctrine applied
/// to the C# engine, which the Python unittest suite cannot reach (audit F-02).
public static class SelfTest
{
    public static string Run(string scenario, int seed)
    {
        var r = new StringBuilder();
        r.AppendLine($"SELF-TEST scenario={scenario} seed={seed}");
        var a = Headless(scenario, seed);
        var b = Headless(scenario, seed);

        Check(r, "deterministic_replay (event stream hash)",
            Exports.Sha256Hex(a.AllJsonl) == Exports.Sha256Hex(b.AllJsonl));
        Check(r, "deterministic_replay (inventory ledger hash)",
            Exports.Sha256Hex(a.InventoryLedgerJson) == Exports.Sha256Hex(b.InventoryLedgerJson));
        Check(r, "no deprecated item.sold", !a.AllJsonl.Contains("item.sold"));
        Check(r, "inventory ledger reconciles", !a.InventoryLedgerJson.Contains("\"reconciles\":false"));
        Check(r, "validation status OK", a.ValidationStatus == "OK");

        bool lifecycleOk = true, ticketOk = true, chronoOk = true;
        var takenByOrder = new Dictionary<string, int>();
        var doneByOrder = new Dictionary<string, int>();
        int lastSeq = 0;
        foreach (var line in a.AllJsonl.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            int seq = ExtractInt(line, "\"sequence\":");
            if (seq <= lastSeq) chronoOk = false;
            lastSeq = seq;
            string type = Extract(line, "\"event_type\":\"");
            string order = Extract(line, "\"order_id\":\"");
            if (type == "item.taken") Bump(takenByOrder, order);
            else if (type == "item.completed")
            {
                Bump(doneByOrder, order);
                if (Get(doneByOrder, order) > Get(takenByOrder, order)) lifecycleOk = false;
            }
            else if (type == "ticket.updated" && line.Contains("\"status\":\"completed\""))
                if (Get(doneByOrder, order) < Get(takenByOrder, order)) ticketOk = false;
        }
        Check(r, "item.taken -> item.completed ordering", lifecycleOk);
        Check(r, "ticket completed only after all items", ticketOk);
        Check(r, "sequence strictly increasing", chronoOk);
        Check(r, "envelope has business_day", a.AllJsonl.Contains("\"business_day\":\"" + SimEvent.BusinessDay + "\""));
        Check(r, "abandonment within band (<=8%)", a.Orders == 0 || (double)a.AbandonedTickets / a.Orders <= 0.08);
        Check(r, "satisfaction computed", a.Csat > 0 && a.Csat <= 100);
        Check(r, "tickets reconcile incl. abandoned", a.Tickets + a.CompletedTickets + a.AbandonedTickets == a.Orders);

        double avgCheck = a.Orders == 0 ? 0 : a.Sales / a.Orders;
        r.AppendLine($"orders={a.Orders} completed={a.CompletedTickets} events={a.EventSeq}");
        r.AppendLine(string.Create(System.Globalization.CultureInfo.InvariantCulture,
            $"avg_check=${avgCheck:0.00} dt_sos={a.MeasuredSosAllDay("drive_thru"):0}s labor={a.LaborPercent:0.0}% csat={a.Csat:0.0} abandoned={a.AbandonedTickets} balked={a.BalkedCars} inspection={a.InspectionScore:0}"));
        return r.ToString();
    }

    static SimRunState Headless(string scenario, int seed)
    {
        var s = new SimRunState { Scenario = scenario, Seed = seed, TimeScale = 1.0, Running = true };
        for (int i = 0; i < 1500 && !s.ShiftEnded; i++) s.Step(60);   // 60 fixed ticks per call
        return s;
    }

    static void Check(StringBuilder r, string name, bool ok) => r.AppendLine((ok ? "[PASS] " : "[FAIL] ") + name);
    static void Bump(Dictionary<string, int> d, string k) { d.TryGetValue(k, out int v); d[k] = v + 1; }
    static int Get(Dictionary<string, int> d, string k) => d.TryGetValue(k, out int v) ? v : 0;

    static string Extract(string line, string key)
    {
        int i = line.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return "";
        i += key.Length;
        int j = line.IndexOf('"', i);
        return j < 0 ? "" : line[i..j];
    }

    static int ExtractInt(string line, string key)
    {
        int i = line.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return -1;
        i += key.Length;
        int j = i;
        while (j < line.Length && char.IsDigit(line[j])) j++;
        return int.TryParse(line[i..j], out int v) ? v : -1;
    }
}
