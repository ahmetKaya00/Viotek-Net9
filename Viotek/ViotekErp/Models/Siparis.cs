using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ViotekErp.Models
{
    [Table("SIPARISLER")]
    public class Siparis
    {
        [Key]
        [Column("sip_Guid")]
        public Guid SipGuid { get; set; }

        [Column("sip_tarih")]
        public DateTime? SipTarih { get; set; }

        [Column("sip_musteri_kod")]
        public string? MusteriKod { get; set; }

        [Column("sip_satici_kod")]
        public string? SaticiKod { get; set; }

        [Column("sip_stok_kod")]
        public string? StokKod { get; set; }

        [Column("sip_miktar")]
        public double? Miktar { get; set; }

        // DİKKAT: sip_tutar çoğu zaman “brüt satır tutarı” (miktar*birim) gibi geliyor
        [Column("sip_tutar")]
        public double? Tutar { get; set; }

        [Column("sip_iptal")]
        public bool? Iptal { get; set; }

        [Column("sip_evrakno_seri")]
        public string? EvrakSeri { get; set; }

        [Column("sip_evrakno_sira")]
        public int? EvrakSira { get; set; }

        [Column("sip_satirno")]
        public int? SatirNo { get; set; }

        [Column("sip_aciklama")]
        public string? Aciklama { get; set; }

        // Birim fiyat
        [Column("sip_b_fiyat")]
        public double? BirimFiyat { get; set; }

        // Döviz
        [Column("sip_doviz_cinsi")]
        public byte? DovizCinsi { get; set; }

        [Column("sip_doviz_kuru")]
        public double? DovizKuru { get; set; }

        [Column("sip_alt_doviz_kuru")]
        public double? AltDovizKuru { get; set; }

        // ✅ ÖNEMLİ: Bunlar yüzde değil, TUTAR iskonto (sende gelen çıktı bunu doğruluyor)
        [Column("sip_iskonto_1")]
        public double? Iskonto1 { get; set; }

        [Column("sip_iskonto_2")]
        public double? Iskonto2 { get; set; }

        [Column("sip_iskonto_3")]
        public double? Iskonto3 { get; set; }

        [Column("sip_iskonto_4")]
        public double? Iskonto4 { get; set; }

        [Column("sip_iskonto_5")]
        public double? Iskonto5 { get; set; }

        [Column("sip_iskonto_6")]
        public double? Iskonto6 { get; set; }
    }
}