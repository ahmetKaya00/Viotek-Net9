using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ViotekErp.Models
{
    [Keyless]
    public class KasaYonetimRow
    {
        [Column("#msg_S_0088")]
        public Guid KayitNo { get; set; }

        [Column("msg_S_0954")]
        public string? KasaTipi { get; set; }

        [Column("msg_S_0955")]
        public string? KasaKodu { get; set; }

        [Column("msg_S_0956")]
        public string? KasaAdi { get; set; }

        [Column("msg_S_0044")]
        public string? MuhasebeKodu { get; set; }

        [Column("msg_S_0957\\T")]
public double? AnaDovizBakiye { get; set; }

[Column("msg_S_1714\\T")]
public double? AlternatifDovizBakiye { get; set; }

[Column("msg_S_0959\\T")]
public double? OrjinalDovizBakiye { get; set; }
        [Column("msg_S_0254")]
        public string? Doviz { get; set; }
    }
}