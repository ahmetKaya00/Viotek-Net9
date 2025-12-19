using System.Collections.Generic;

namespace ViotekErp.Models
{
    public class KasalarViewModel
    {
        public string? Search { get; set; }
        public List<KasaYonetimRow> Items { get; set; } = new();
        public double ToplamAna { get; set; }
public double ToplamOrjinal { get; set; }
    }
}