using System;
using System.Collections.Generic;

namespace ViotekErp.Models
{
    public class SiparisDetaySatirDto
    {
        public int? SatirNo { get; set; }
        public string? StokKod { get; set; }
        public string? UrunAdi { get; set; }
        public string? Aciklama { get; set; }

        public double? Miktar { get; set; }

        public double? BirimFiyat { get; set; }     // döviz birim fiyat
        public double? IskontoYuzde { get; set; }   // hesaplanmış %
        public double? NetBirimFiyat { get; set; }  // döviz net birim

        public string? Doviz { get; set; }
        public double? Kur { get; set; }            // sip_doviz_kuru (TL ise 1)

        public double? TutarTL { get; set; }        // net TL tutar
    }

    public class SiparisDetayViewModel
    {
        public string? EvrakSeri { get; set; }
        public int EvrakSira { get; set; }
        public DateTime? Tarih { get; set; }

        public string? MusteriKod { get; set; }
        public string? MusteriAd { get; set; }

        public string? SaticiKod { get; set; }
        public string? SaticiAd { get; set; }

        public double ToplamMiktar { get; set; }
        public double GenelToplamTL { get; set; }       // KDV hariç
        public double GenelToplamKdvliTL { get; set; }  // KDV dahil (%20)

        public List<SiparisDetaySatirDto> Satirlar { get; set; } = new();
    }
}