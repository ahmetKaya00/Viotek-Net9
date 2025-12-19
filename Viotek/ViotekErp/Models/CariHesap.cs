using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ViotekErp.Models
{
    [Table("CARI_HESAPLAR")]
    public class CariHesap
    {
        [Key]
        [Column("cari_Guid")]
        public Guid CariGuid { get; set; }

        [Column("cari_kod")]
        public string CariKod { get; set; } = null!;

        [Column("cari_unvan1")]
        public string? Unvan1 { get; set; }
    }
}
