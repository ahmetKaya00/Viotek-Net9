using System;
using System.Collections.Generic;

namespace ViotekErp.Models
{
    public class SatisAnalizViewModel
    {
        public string Period { get; set; } = "month"; // day | week | month | year
        public DateTime RefDate { get; set; } = DateTime.Today;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; } // inclusive

        // Özet
        public decimal TotalAmount { get; set; }
        public decimal TotalQuantity { get; set; }
        public int TotalRows { get; set; }

        // Enler
        public TopSeller? TopSeller { get; set; }
        public TopCustomer? TopCustomer { get; set; }
        public TopProduct? TopProduct { get; set; }

        // Ek içgörü
        public TopCustomerForTopSeller? TopCustomerForTopSeller { get; set; }
        public List<SellerTopProduct> SellersTopProducts { get; set; } = new();

        // ✅ Kod→İsim sözlükleri
        public Dictionary<string, string> SellerNames { get; set; } = new();
        public Dictionary<string, string> CustomerNames { get; set; } = new();
        public Dictionary<string, string> StockNames { get; set; } = new();
    }

    public class TopSeller
    {
        public string SellerCode { get; set; } = "";
        public string SellerName { get; set; } = ""; // ✅
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
        public int Rows { get; set; }
    }

    public class TopCustomer
    {
        public string CustomerCode { get; set; } = "";
        public string CustomerName { get; set; } = ""; // ✅
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
        public int Rows { get; set; }
    }

    public class TopProduct
    {
        public string StockCode { get; set; } = "";
        public string StockName { get; set; } = ""; // ✅
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
        public int Rows { get; set; }
    }

    public class TopCustomerForTopSeller
    {
        public string SellerCode { get; set; } = "";
        public string SellerName { get; set; } = ""; // ✅
        public string CustomerCode { get; set; } = "";
        public string CustomerName { get; set; } = ""; // ✅
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
        public int Rows { get; set; }
    }

    public class SellerTopProduct
    {
        public string SellerCode { get; set; } = "";
        public string SellerName { get; set; } = ""; // ✅
        public string StockCode { get; set; } = "";
        public string StockName { get; set; } = ""; // ✅
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
        public int Rows { get; set; }
    }
}