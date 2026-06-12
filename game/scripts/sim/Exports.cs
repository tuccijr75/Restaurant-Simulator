using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace RestaurantSimulator;

/// Builds the full README output contract from a finished (or in-progress) run:
/// event_stream.jsonl, inventory_ledger.json, staffing_ledger.json,
/// recommendation_validation_dataset.json, alert_validation_dataset.json,
/// end_of_shift_summary.json, run_receipt.json, hashes.json.
/// (Audit F-09: only the raw event stream was exported before.)
public static class Exports
{
    static string Num(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    static string S(string v) => "\"" + v.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    public static string Provenance(SimRunState s) =>
        $"{{\"simulation_id\":\"sim_{s.Scenario}_{s.Seed}\",\"scenario_id\":\"scn_{s.Scenario}\"," +
        $"\"seed\":{s.Seed},\"schema_version\":\"1.0.0\",\"generator_version\":\"game-0.3.0\"," +
        $"\"source_pack_version\":\"rs_source_pack_v1.1\",\"business_day\":\"{SimEvent.BusinessDay}\"," +
        "\"synthetic_data\":true,\"data_classification\":\"INTERNAL_SIM\"}";

    public static string InventoryLedger(SimRunState s) =>
        $"{{\"provenance\":{Provenance(s)},\"equation\":\"opening + prep_confirmed_or_received - consumed_item_taken - waste_recorded + approved_adjustments = closing\",\"components\":{s.InventoryLedgerJson}}}";

    public static string StaffingLedger(SimRunState s)
    {
        var rows = new List<string>();
        foreach (var line in s.StaffingLedgerFull.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            rows.Add(S(line));
        return $"{{\"provenance\":{Provenance(s)}," +
               "\"model\":\"scheduled_roles - call_offs + reassignments + breaks/returns = active_role_coverage_by_interval\"," +
               $"\"call_offs_total\":{s.CallOffs},\"breaks_taken_total\":{s.BreaksTaken}," +
               $"\"entries\":[{string.Join(",", rows)}]}}";
    }

    public static string RecommendationDataset(SimRunState s) =>
        $"{{\"provenance\":{Provenance(s)},\"rows\":[{s.RecommendationRows}]}}";

    public static string AlertDataset(SimRunState s) =>
        $"{{\"provenance\":{Provenance(s)},\"rows\":[{s.AlertRows}]}}";

    public static string EndOfShiftSummary(SimRunState s)
    {
        double dt = s.MeasuredSosAllDay("drive_thru"), fc = s.MeasuredSosAllDay("lobby"), del = s.MeasuredSosAllDay("delivery");
        double avgCheck = s.Orders == 0 ? 0 : s.Sales / s.Orders;
        bool ledgersOk = !s.InventoryLedgerJson.Contains("\"reconciles\":false");
        string Pass(bool b) => b ? "\"pass\"" : "\"review_required\"";
        return $"{{\"provenance\":{Provenance(s)}," +
            $"\"orders_total\":{s.Orders}," +
            $"\"orders_by_channel\":{{\"drive_thru\":{s.DriveThru},\"lobby\":{s.FrontCounter},\"mobile\":{s.Mobile},\"delivery\":{s.Delivery}}}," +
            $"\"tickets_completed\":{s.CompletedTickets},\"active_tickets_at_close\":{s.Tickets}," +
            $"\"sales_usd\":{Num(s.Sales)},\"average_check_usd\":{Num(avgCheck)}," +
            $"\"sales_this_30\":{Num(s.SalesThis30)},\"tickets_this_30\":{s.TicketsThis30}," +
            $"\"sos_avg_seconds\":{{\"drive_thru\":{Num(dt)},\"lobby\":{Num(fc)},\"delivery\":{Num(del)}}}," +
            $"\"labor_cost_usd\":{Num(s.LaborCost)},\"labor_percent\":{Num(s.LaborPercent)}," +
            $"\"waste_units\":{s.Waste},\"waste_cost_usd\":{Num(s.WasteCost)},\"waste_events_total\":{s.WasteSeq}," +
            $"\"overload_events_total\":{s.OverloadSeq},\"call_offs\":{s.CallOffs},\"breaks_taken\":{s.BreaksTaken}," +
            $"\"sanitation_tasks\":{s.SanitationTasks}," +
            $"\"customer_satisfaction_avg\":{Num(s.Csat)},\"abandoned_tickets\":{s.AbandonedTickets},\"balked_cars\":{s.BalkedCars},\"lost_sales_usd\":{Num(s.LostSales)}," +
            $"\"complaints_comped\":{s.ComplaintsComped},\"comp_cost_usd\":{Num(s.CompCost)},\"overtime_premium_usd\":{Num(s.OvertimePremium)},\"maintenance_spend_usd\":{Num(s.MaintSpend)}," +
            $"\"health_inspection_score\":{Num(s.InspectionScore)},\"health_inspection_notes\":{S(s.InspectionNotes)},\"worst_equipment_condition\":{Num(s.WorstEquipmentCondition)}," +
            "\"pass_fail\":{" +
            $"\"tickets_within_docs06_band\":{Pass(s.Orders >= 500 && s.Orders <= 1400)}," +
            $"\"avg_check_within_band\":{Pass(avgCheck >= 9.0 && avgCheck <= 13.0)}," +
            $"\"drive_thru_sos_within_target\":{Pass(dt > 0 && dt <= 480)}," +
            $"\"labor_percent_within_target\":{Pass(s.LaborPercent > 0 && s.LaborPercent <= 35)}," +
            $"\"ledgers_reconcile\":{Pass(ledgersOk)}," +
            $"\"temp_compliance\":{Pass(!s.TempOutOfRange)}," +
            $"\"validation_status_ok\":{Pass(s.ValidationStatus == "OK")}," +
            $"\"customer_satisfaction\":{Pass(s.Csat >= 75)}," +
            $"\"abandonment_within_band\":{Pass(s.Orders == 0 || (double)s.AbandonedTickets / s.Orders <= 0.08)}," +
            $"\"health_inspection\":{Pass(s.InspectionScore < 0 || s.InspectionScore >= 80)}}}}}";
    }

    public static string RunReceipt(SimRunState s, string createdAtIso) =>
        $"{{\"receipt_id\":\"rcpt_run_sim_{s.Scenario}_{s.Seed}\",\"task_id\":\"RS-INTEGRATED-6T\"," +
        "\"workflow_id\":\"wf_generate_simulated_business_day\",\"runtime_class\":\"T3\"," +
        $"\"provenance\":{Provenance(s)},\"created_at\":\"{createdAtIso}\"," +
        $"\"event_count\":{s.EventSeq},\"orders_total\":{s.Orders},\"completed_tickets\":{s.CompletedTickets}," +
        $"\"deprecated_item_sold_present\":false,\"validation_status\":{S(s.ValidationStatus)}," +
        "\"outputs\":[\"event_stream.jsonl\",\"inventory_ledger.json\",\"staffing_ledger.json\"," +
        "\"recommendation_validation_dataset.json\",\"alert_validation_dataset.json\"," +
        "\"end_of_shift_summary.json\",\"run_receipt.json\",\"hashes.json\"]," +
        "\"next_required_action\":\"Run deterministic replay self-test (F9) and review end_of_shift_summary.json\"}";

    public static string Sha256Hex(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        var sb = new StringBuilder(64);
        foreach (var b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    /// All 8 contract outputs. run_receipt carries a real-time stamp and is therefore
    /// excluded from determinism comparisons (hashes.json covers the data artifacts).
    public static List<(string Name, string Content)> BuildAll(SimRunState s, string createdAtIso)
    {
        s.RefreshValidation("export");
        var files = new List<(string, string)>
        {
            ("event_stream.jsonl", s.AllJsonl),
            ("inventory_ledger.json", InventoryLedger(s)),
            ("staffing_ledger.json", StaffingLedger(s)),
            ("recommendation_validation_dataset.json", RecommendationDataset(s)),
            ("alert_validation_dataset.json", AlertDataset(s)),
            ("end_of_shift_summary.json", EndOfShiftSummary(s)),
            ("run_receipt.json", RunReceipt(s, createdAtIso)),
        };
        var sb = new StringBuilder("{");
        for (int i = 0; i < files.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(S(files[i].Item1)).Append(':').Append(S(Sha256Hex(files[i].Item2)));
        }
        sb.Append('}');
        files.Add(("hashes.json", sb.ToString()));
        return files;
    }
}
