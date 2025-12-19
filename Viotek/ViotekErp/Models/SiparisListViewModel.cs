using System;
using System.Collections.Generic;

namespace ViotekErp.Models
{
    // ✅ Liste ekranında her EVRAK için tek satır özet
    public class SiparisOzetDto
    {
        public string? EvrakSeri { get; set; }
        public int EvrakSira { get; set; }

        public DateTime? Tarih { get; set; }

        public string? MusteriKod { get; set; }
        public string? SaticiKod { get; set; }

        public double ToplamMiktar { get; set; }
        public double ToplamTutarTl { get; set; } // KDV hariç, TL
    }

    public class SiparisListViewModel
    {
        public List<SiparisOzetDto> Items { get; set; } = new();

        // Filtreler
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Search { get; set; }

        // Sayfalama
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }   // 10 / 18 / 25 / 50

        // Özet
        public int TotalCount { get; set; }
        public double TotalAmount { get; set; }        // KDV Hariç (TL)
        public double TotalAmountKdvli { get; set; }   // KDV Dahil (TL)
        public double TotalQuantity { get; set; }

        public Dictionary<string, string> MusteriAdlari { get; set; } = new();
        public Dictionary<string, string> SaticiAdlari { get; set; } = new();
    }
}