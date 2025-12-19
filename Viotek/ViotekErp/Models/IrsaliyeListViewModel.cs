using System;
using System.Collections.Generic;

namespace ViotekErp.Models
{
    public class IrsaliyeListViewModel
    {
        public List<IrsaliyeListRow> Items { get; set; } = new();

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Search { get; set; }
        public string Status { get; set; } = "all"; // all | invoiced | waiting

        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalCount { get; set; } = 0;
    }

    public class IrsaliyeListRow
    {
        public DateTime? Tarih { get; set; }
        public string EvrakSeri { get; set; } = "";
        public int EvrakSira { get; set; }

        public string CariKod { get; set; } = "";
        public string CariUnvan { get; set; } = "";

        public string IlkUrunAdi { get; set; } = "";
        public int SatirSayisi { get; set; }

        public decimal ToplamTutar { get; set; }

        public string FaturaDurumu { get; set; } = ""; // Faturalandı / Fatura Bekliyor
    }
}