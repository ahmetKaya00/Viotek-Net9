using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ViotekErp.Models
{
    [Table("TBL_SERVIS")]
    public class TblServis
    {
        [Key]
        [Column("SERVIS_ID")]
        public int ServisId { get; set; }

        [Column("SERVIS_TARIH")]
        public DateTime? ServisTarih { get; set; }

        [Column("SERVIS_CARI_KOD")]
        public string? ServisCariKod { get; set; }

        [Column("SERVIS_STOK_KOD")]
        public string? ServisStokKod { get; set; }

        [Column("SERVIS_SERI_NUMARA")]
        public string? ServisSeriNumara { get; set; }

        [Column("SERVIS_SATINALMA_TARIH")]
        public DateTime? ServisSatinalmaTarih { get; set; }

        [Column("SERVIS_GARANTI")]
        public int? ServisGaranti { get; set; }

        [Column("SERVIS_TESLIM_EDEN")]
        public string? ServisTeslimEden { get; set; }

        [Column("SERVIS_TESLIM_ALAN")]
        public string? ServisTeslimAlan { get; set; }

        [Column("SERVIS_ARIZA_ACIKLAMA")]
        public string? ServisArizaAciklama { get; set; }

        [Column("TED_GONDERIM_TARIH")]
        public DateTime? TedGonderimTarih { get; set; }

        [Column("TED_GONDERIM_CARI_KOD")]
        public string? TedGonderimCariKod { get; set; }

        [Column("TED_GONDERIM_ACIKLAMA")]
        public string? TedGonderimAciklama { get; set; }

        [Column("TED_ALIM_TARIH")]
        public DateTime? TedAlimTarih { get; set; }

        [Column("TED_ALIM_YAPILAN_ISLEM")]
        public string? TedAlimYapilanIslem { get; set; }

        [Column("TED_ALIM_SERI_NUMARA")]
        public string? TedAlimSeriNumara { get; set; }

        [Column("TED_ALIM_TESLIM_ALAN")]
        public string? TedAlimTeslimAlan { get; set; }

        [Column("MUS_TESLIM_TARIH")]
        public DateTime? MusTeslimTarih { get; set; }

        [Column("MUS_YAPILAN_ISLEM")]
        public string? MusYapilanIslem { get; set; }

        [Column("MUS_SERI_NUMARA")]
        public string? MusSeriNumara { get; set; }

        [Column("MUS_TESLIM_EDEN")]
        public string? MusTeslimEden { get; set; }

        [Column("MUS_TESLIM_ALAN")]
        public string? MusTeslimAlan { get; set; }

        [Column("SERVIS_DURUM")]
        public byte? ServisDurum { get; set; }

        [Column("SERVIS_TAMAMLANDI")]
        public bool? ServisTamamlandi { get; set; }

        [Column("SERVIS_AKTIF")]
        public bool? ServisAktif { get; set; }

        [Column("INSERT_ID")]
        public int? InsertId { get; set; }

        [Column("INSERT_DATE")]
        public DateTime? InsertDate { get; set; }

        [Column("UPDATE_ID")]
        public int? UpdateId { get; set; }

        [Column("UPDATE_DATE")]
        public DateTime? UpdateDate { get; set; }

        // ✅ DB kolon eşlemesi önemli
        [Column("SERVIS_GIREN_SATICI_KOD")]
        public string? ServisGirenSaticiKod { get; set; }
    }
}