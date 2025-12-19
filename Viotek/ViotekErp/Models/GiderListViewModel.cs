using System;
using System.Collections.Generic;

namespace ViotekErp.Models
{
    public class GiderListItemDto
    {
        public Guid? MsgS0088 { get; set; }        // #msg_S_0088 (ID)
        public DateTime? MsgS0089 { get; set; }    // msg_S_0089 (Tarih)
        public string? MsgS0223 { get; set; }      // msg_S_0223 (Dönem/Açıklama)
        public string? MsgS0433 { get; set; }      // msg_S_0433 (Tip/Grup)
        public string? MsgS1167 { get; set; }      // msg_S_1167 (Kod)
        public string? MsgS1168 { get; set; }      // msg_S_1168 (Gider Adı)
        public double? MsgS1164 { get; set; }      // msg_S_1164 (TL Tutar)
        public string? MsgS1160 { get; set; }      // msg_S_1160 (Döviz)
        public double? MsgS1165 { get; set; }      // msg_S_1165 (Döviz Tutarı)
    }

    public class PiePoint
    {
        public string Label { get; set; } = "";
        public double Value { get; set; }
    }

    public class SeriesPoint
    {
        public string Label { get; set; } = ""; // 01.11 gibi
        public double Value { get; set; }
    }

    public class GiderListViewModel
    {
        public List<GiderListItemDto> Items { get; set; } = new();

        public string? Search { get; set; }
        public string Sort { get; set; } = "tarih_desc";
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalCount { get; set; }

        // ✅ Zaman filtresi
        public string Period { get; set; } = "month";  // "all" | "month"
        public string? Month { get; set; }             // "2025-11"
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // ✅ Özet
        public double TotalAmountNet { get; set; }      // SUM(tutar)
        public double TotalAmountAbs { get; set; }      // SUM(ABS(tutar))
        public int NegativeCount { get; set; }
        public int PositiveCount { get; set; }

        // ✅ Grafik
        public List<SeriesPoint> DailyTotalsAbs { get; set; } = new(); // günlük mutlak toplam
        public List<PiePoint> PieByExpenseName { get; set; } = new();
    }
}