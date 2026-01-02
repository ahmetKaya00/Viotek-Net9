using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ViotekErp.Models
{
    [Table("erp_kullanici")]
    public class erp_kullanici
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string KullaniciAdi { get; set; } = "";

        [StringLength(100)]
        public string? AdSoyad { get; set; }

        [Required, StringLength(20)]
        public string Rol { get; set; } = "User"; // Admin/User

        [Required, StringLength(200)]
        public string SifreHash { get; set; } = ""; // düz şifre tutulacak

        public bool AktifMi { get; set; } = true;

        public DateTime? SonGirisTarihi { get; set; }

        public bool SifreDegistir { get; set; } = false;

        public DateTime OlusturmaTarihi { get; set; } = DateTime.UtcNow;

        public DateTime? GuncellemeTarihi { get; set; }
        public string? SipSaticiKod { get; set; } // SIPARISLER.sip_satici_kod ile eşleşecek
    }
}