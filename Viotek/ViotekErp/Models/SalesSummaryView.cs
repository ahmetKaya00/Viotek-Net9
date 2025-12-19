using Microsoft.EntityFrameworkCore;

namespace ViotekErp.Models
{
    [Keyless] // using Microsoft.EntityFrameworkCore;
    public class SalesSummaryView
    {
        public int Yil { get; set; }
        public int Ay { get; set; }
        public decimal ToplamTutar { get; set; }
    }
}
