using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ViotekErp.Models
{
    [Table("SATICILAR_VIEW")]
    public class Satici
    {
        [Key]
        public string SaticiKod { get; set; } = null!;   // CAST(User_no AS nvarchar)

        public string? SaticiAd { get; set; }           // User_LongName / User_name
    }
}
