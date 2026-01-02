using System;
using System.Collections.Generic;

namespace ViotekErp.Models
{
    public static class ServisTabs
    {
        public const string Acilan = "acilan";
        public const string Tedarikcide = "tedarikcide";
        public const string TedarikcidenGelen = "tedarikciden-gelen";
        public const string TeslimEdilen = "teslim-edilen";

        public static byte ToDurum(string tab)
        {
            tab = (tab ?? "").Trim().ToLowerInvariant();
            return tab switch
            {
                Acilan => (byte)1,
                Tedarikcide => (byte)2,
                TedarikcidenGelen => (byte)3,
                TeslimEdilen => (byte)4,
                _ => (byte)1
            };
        }
    }

    public class ServisIndexVm
    {
        public string ActiveTab { get; set; } = ServisTabs.Acilan;
        public string? Search { get; set; }

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public string? CurrentSipSaticiKod { get; set; }
        public string? CurrentSipSaticiAd { get; set; }
    }

    public class ServisListVm
    {
        public string ActiveTab { get; set; } = ServisTabs.Acilan;
        public string? Search { get; set; }

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public List<TblServis> Items { get; set; } = new();

        public Dictionary<string, string> CariAdlari { get; set; } = new();
        public Dictionary<string, string> StokAdlari { get; set; } = new();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class ServisEditVm
    {
        public TblServis Item { get; set; } = new();

        public string? CariAd { get; set; }
        public string? StokAd { get; set; }

        public string? CurrentSipSaticiKod { get; set; }
        public string? CurrentSipSaticiAd { get; set; }
    }
}