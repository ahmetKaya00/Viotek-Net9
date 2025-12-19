using System;
using System.Collections.Generic;

namespace ViotekErp.Models
{
    // STOKLAR tablosunun basit entity'si
    public class Stok
    {
        public string StoKod { get; set; } = null!;
        public string? StoIsim { get; set; }
        public string? MarkaKodu { get; set; }
        public string? KategoriKodu { get; set; }
    }

    // VW_STOK_MEVCUT view'i için model
    public class StokMevcut
    {
        public string StoKod { get; set; } = null!;
        public string? StoIsim { get; set; }
        public string? StoBirim1Ad { get; set; }
        public double MevcutMiktar { get; set; }
    }

    // STOK_HAREKETLERI için basit entity
    public class StokHareket
    {
        public Guid SthGuid { get; set; }
        public DateTime? SthTarih { get; set; }
        public string? StokKod { get; set; }
        public double? Miktar { get; set; }
        public double? Tutar { get; set; }
        public string? EvrakSeri { get; set; }
        public int? EvrakSira { get; set; }
        public string? CariKodu { get; set; }
        public string? PlasiyerKodu { get; set; }
        public int? GirisDepoNo { get; set; }
        public int? CikisDepoNo { get; set; }
        public byte? Tip { get; set; }   // sth_tip
    }

    // Liste satırı DTO'su (Index tablosu için)
    public class StokListItemDto
    {
        public string StokKod { get; set; } = "";
        public string StokIsim { get; set; } = "";
        public double MevcutMiktar { get; set; }
        public DateTime? SonHareketTarihi { get; set; }
        public string? MarkaKodu { get; set; }
        public string? KategoriKodu { get; set; }
        public double? SonSatisFiyati { get; set; }
    }

    // Liste ViewModel
    public class StokListViewModel
    {
        public List<StokListItemDto> Items { get; set; } = new();

        public string? Search { get; set; }
        public double? MinMiktar { get; set; }

        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public double TotalMevcut { get; set; }
    }

    // Detay popup header + satırlar
    public class StokDetayViewModel
    {
        public string StokKod { get; set; } = "";
        public string? StokIsim { get; set; }
        public double MevcutMiktar { get; set; }

        public DateTime? SonHareketTarihi { get; set; }
        public string? MarkaKodu { get; set; }
        public string? KategoriKodu { get; set; }
        public double? SonSatisFiyati { get; set; }

        public List<StokHareketDetayDto> Hareketler { get; set; } = new();
    }

    public class StokHareketDetayDto
    {
        public DateTime? Tarih { get; set; }
        public string? EvrakNo { get; set; }
        public string? TipAciklama { get; set; }
        public double? Miktar { get; set; }
        public double? Tutar { get; set; }
        public string? CariKodu { get; set; }
        public string? CariAd { get; set; }          // ✅ eklendi
        public string? PlasiyerKodu { get; set; }
        public string? PlasiyerAd { get; set; }      // ✅ eklendi
        public int? GirisDepoNo { get; set; }
        public int? CikisDepoNo { get; set; }
    }
}
