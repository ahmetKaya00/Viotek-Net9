using System;
using System.Collections.Generic;

namespace ViotekErp.Models
{
    public class DashboardViewModel
    {
        public List<WeeklySalesPoint> ChartPoints { get; set; } = new(); // generic: Label + Amount
        // ✅ Filtre UI
        public string Period { get; set; } = "month"; // day/week/month/year/custom
        public DateTime? FilterStartDate { get; set; }
        public DateTime? FilterEndDate { get; set; }

        // KARTLAR
        public double MonthlySalesTotal { get; set; }       // (artık seçilen aralık toplamı gibi düşünebilirsin)
        public int TodayOrderCount { get; set; }

        public string? EmployeeOfMonthName { get; set; }
        public double EmployeeOfMonthSales { get; set; }

        public List<WeeklySalesPoint> WeeklySales { get; set; } = new();

        public double TotalSalesAllTime { get; set; }
        public int TotalOrderCount { get; set; }
        public DateTime? FirstOrderDate { get; set; }
        public DateTime? LastOrderDate { get; set; }

        public double Last30DaysSalesTotal { get; set; }
        public int Last30DaysOrderCount { get; set; }

        public List<DailySalesPoint> Last30DaysSales { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();

        public List<DashboardLastOrderDto> LastOrders { get; set; } = new();

        public Dictionary<string, string> LastOrdersMusteriAdlari { get; set; } = new();
        public Dictionary<string, string> LastOrdersSaticiAdlari { get; set; } = new();
    }

    // ✅ Son 10 sipariş DTO'su (TutarTL kullanacağız)
    public class DashboardLastOrderDto
{
    public DateTime? SipTarih { get; set; }

    public string? EvrakSeri { get; set; }
    public int? EvrakSira { get; set; }

    public string? MusteriKod { get; set; }
    public string? SaticiKod { get; set; }

    public double? ToplamMiktar { get; set; }
    public double? Tutar { get; set; } // TL net (iskonto + kur)
    public double? TutarKdvliTL { get; set; } // TL net (iskonto + kur)


    public int KalemSayisi { get; set; } // aynı evrak içindeki satır sayısı
}

    public class WeeklySalesPoint
    {
        public string DayLabel { get; set; } = "";
        public double Amount { get; set; }
    }

    public class DailySalesPoint
    {
        public string DayLabel { get; set; } = "";
        public double Amount { get; set; }
    }

    public class TopProductDto
    {
        public string StokKod { get; set; } = "";
        public double Quantity { get; set; }
    }
}