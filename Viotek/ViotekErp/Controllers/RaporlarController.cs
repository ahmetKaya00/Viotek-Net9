using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using ViotekErp.Data;
using ViotekErp.Models;

namespace ViotekErp.Controllers
{
    public class RaporlarController : Controller
    {
        private readonly MikroDbContext _db;

        public RaporlarController(MikroDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> SatisAnaliz(string period = "month", DateTime? refDate = null)
        {
            period = NormalizePeriod(period);

            var maxDate = await GetMaxSiparisTarih();
            var rd = (refDate ?? maxDate ?? DateTime.Today).Date;

            var (start, end) = GetRange(period, rd);

            var vm = new SatisAnalizViewModel
            {
                Period = period,
                RefDate = rd,
                StartDate = start,
                EndDate = end
            };

            await FillSummary(vm);
            await FillTopSeller(vm);
            await FillTopCustomer(vm);
            await FillTopProduct(vm);
            await FillTopCustomerForTopSeller(vm);
            await FillSellersTopProducts(vm);

            // ✅ Kod→İsim
            await FillNames(vm);

            return View(vm);
        }

        // -------------------- İSİM ÇÖZÜMLEME (SiparislerController mantığı) --------------------
        private async Task FillNames(SatisAnalizViewModel vm)
        {
            var sellerCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var customerCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stockCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (vm.TopSeller != null && !string.IsNullOrWhiteSpace(vm.TopSeller.SellerCode))
                sellerCodes.Add(vm.TopSeller.SellerCode);

            if (vm.TopCustomer != null && !string.IsNullOrWhiteSpace(vm.TopCustomer.CustomerCode))
                customerCodes.Add(vm.TopCustomer.CustomerCode);

            if (vm.TopProduct != null && !string.IsNullOrWhiteSpace(vm.TopProduct.StockCode))
                stockCodes.Add(vm.TopProduct.StockCode);

            if (vm.TopCustomerForTopSeller != null)
            {
                if (!string.IsNullOrWhiteSpace(vm.TopCustomerForTopSeller.SellerCode))
                    sellerCodes.Add(vm.TopCustomerForTopSeller.SellerCode);

                if (!string.IsNullOrWhiteSpace(vm.TopCustomerForTopSeller.CustomerCode))
                    customerCodes.Add(vm.TopCustomerForTopSeller.CustomerCode);
            }

            if (vm.SellersTopProducts != null && vm.SellersTopProducts.Count > 0)
            {
                foreach (var row in vm.SellersTopProducts)
                {
                    if (!string.IsNullOrWhiteSpace(row.SellerCode))
                        sellerCodes.Add(row.SellerCode);

                    if (!string.IsNullOrWhiteSpace(row.StockCode))
                        stockCodes.Add(row.StockCode);
                }
            }

            var sellerList = new List<string>(sellerCodes);
            var customerList = new List<string>(customerCodes);
            var stockList = new List<string>(stockCodes);

            if (sellerList.Count > 0)
            {
                var saticiRows = await _db.SorumluAdlari
                    .AsNoTracking()
                    .Where(x => sellerList.Contains(x.SaticiKod))
                    .ToListAsync();

                vm.SellerNames = saticiRows.ToDictionary(
                    x => x.SaticiKod,
                    x => string.IsNullOrWhiteSpace(x.SaticiAd) ? x.SaticiKod : x.SaticiAd
                );
            }

            if (customerList.Count > 0)
            {
                var cariRows = await _db.CariHesaplar
                    .AsNoTracking()
                    .Where(c => customerList.Contains(c.CariKod))
                    .ToListAsync();

                vm.CustomerNames = cariRows.ToDictionary(
                    c => c.CariKod,
                    c => string.IsNullOrWhiteSpace(c.Unvan1) ? c.CariKod : c.Unvan1!
                );
            }

            if (stockList.Count > 0)
            {
                var stokRows = await _db.StokAdlari
                    .AsNoTracking()
                    .Where(s => stockList.Contains(s.StokKod))
                    .ToListAsync();

                vm.StockNames = stokRows.ToDictionary(
                    s => s.StokKod,
                    s => string.IsNullOrWhiteSpace(s.StokAd) ? s.StokKod : s.StokAd
                );
            }

            if (vm.TopSeller != null)
                vm.TopSeller.SellerName = Resolve(vm.SellerNames, vm.TopSeller.SellerCode);

            if (vm.TopCustomer != null)
                vm.TopCustomer.CustomerName = Resolve(vm.CustomerNames, vm.TopCustomer.CustomerCode);

            if (vm.TopProduct != null)
                vm.TopProduct.StockName = Resolve(vm.StockNames, vm.TopProduct.StockCode);

            if (vm.TopCustomerForTopSeller != null)
            {
                vm.TopCustomerForTopSeller.SellerName = Resolve(vm.SellerNames, vm.TopCustomerForTopSeller.SellerCode);
                vm.TopCustomerForTopSeller.CustomerName = Resolve(vm.CustomerNames, vm.TopCustomerForTopSeller.CustomerCode);
            }

            if (vm.SellersTopProducts != null)
            {
                foreach (var row in vm.SellersTopProducts)
                {
                    row.SellerName = Resolve(vm.SellerNames, row.SellerCode);
                    row.StockName = Resolve(vm.StockNames, row.StockCode);
                }
            }
        }

        private static string Resolve(Dictionary<string, string> dict, string code)
            => (!string.IsNullOrWhiteSpace(code) && dict.TryGetValue(code, out var name)) ? name : code;

        // -------------------- PERIOD --------------------
        private static string NormalizePeriod(string? period)
        {
            period = (period ?? "month").Trim().ToLowerInvariant();
            return period switch
            {
                "day" => "day",
                "week" => "week",
                "month" => "month",
                "year" => "year",
                _ => "month"
            };
        }

        private static (DateTime start, DateTime endInclusive) GetRange(string period, DateTime refDate)
        {
            refDate = refDate.Date;

            return period switch
            {
                "day" => (refDate, refDate),

                "week" => (
                    StartOfWeek(refDate, DayOfWeek.Monday),
                    StartOfWeek(refDate, DayOfWeek.Monday).AddDays(6)
                ),

                "month" => (
                    new DateTime(refDate.Year, refDate.Month, 1),
                    new DateTime(refDate.Year, refDate.Month, 1).AddMonths(1).AddDays(-1)
                ),

                "year" => (
                    new DateTime(refDate.Year, 1, 1),
                    new DateTime(refDate.Year, 12, 31)
                ),

                _ => (
                    new DateTime(refDate.Year, refDate.Month, 1),
                    new DateTime(refDate.Year, refDate.Month, 1).AddMonths(1).AddDays(-1)
                )
            };
        }

        private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
        {
            int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
            return date.AddDays(-diff).Date;
        }

        // -------------------- DB (RAW SQL) --------------------
        private async Task<DateTime?> GetMaxSiparisTarih()
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT MAX(sip_tarih) FROM dbo.SIPARISLER WITH (NOLOCK)";
                var obj = await cmd.ExecuteScalarAsync();
                if (obj == null || obj == DBNull.Value) return null;
                return Convert.ToDateTime(obj);
            }
            finally
            {
                if (conn.State == ConnectionState.Open) await conn.CloseAsync();
            }
        }

        // ✅ NET TL EXPRESSION:
        // (sip_tutar - iskontolar) * kur
        // kur: doviz_cinsi=0 => 1 else sip_doviz_kuru
        private const string NetTlExpr = @"
(
    (
        CAST(ISNULL(sip_tutar,0) AS decimal(18,6))
        -
        (
            CAST(ISNULL(sip_iskonto_1,0) AS decimal(18,6)) +
            CAST(ISNULL(sip_iskonto_2,0) AS decimal(18,6)) +
            CAST(ISNULL(sip_iskonto_3,0) AS decimal(18,6)) +
            CAST(ISNULL(sip_iskonto_4,0) AS decimal(18,6)) +
            CAST(ISNULL(sip_iskonto_5,0) AS decimal(18,6)) +
            CAST(ISNULL(sip_iskonto_6,0) AS decimal(18,6))
        )
    )
    *
    (CASE WHEN ISNULL(sip_doviz_cinsi,0) = 0 THEN 1 ELSE ISNULL(sip_doviz_kuru,1) END)
)
";

        private async Task FillSummary(SatisAnalizViewModel vm)
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
SELECT
    COUNT(1) AS SatirAdedi,
    SUM({NetTlExpr}) AS ToplamTutarNetTL,
    SUM(CAST(ISNULL(sip_miktar,0) AS decimal(18,2))) AS ToplamMiktar
FROM dbo.SIPARISLER WITH (NOLOCK)
WHERE
    (sip_iptal IS NULL OR sip_iptal = 0)
    AND sip_tarih >= @p1 AND sip_tarih < @p2;
";
                AddDateRangeParams(cmd, vm.StartDate, vm.EndDate);

                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    vm.TotalRows = r.IsDBNull(0) ? 0 : Convert.ToInt32(r.GetValue(0));
                    vm.TotalAmount = r.IsDBNull(1) ? 0m : Convert.ToDecimal(r.GetValue(1)); // ✅ artık Net TL
                    vm.TotalQuantity = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2));
                }
            }
            finally
            {
                if (conn.State == ConnectionState.Open) await conn.CloseAsync();
            }
        }

        private async Task FillTopSeller(SatisAnalizViewModel vm)
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
SELECT TOP 1
    ISNULL(sip_satici_kod,'') AS SellerCode,
    SUM({NetTlExpr}) AS AmountNetTL,
    SUM(CAST(ISNULL(sip_miktar,0) AS decimal(18,2))) AS Qty,
    COUNT(1) AS Rows
FROM dbo.SIPARISLER WITH (NOLOCK)
WHERE
    (sip_iptal IS NULL OR sip_iptal = 0)
    AND sip_tarih >= @p1 AND sip_tarih < @p2
GROUP BY ISNULL(sip_satici_kod,'')
ORDER BY AmountNetTL DESC;
";
                AddDateRangeParams(cmd, vm.StartDate, vm.EndDate);

                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    vm.TopSeller = new TopSeller
                    {
                        SellerCode = r.IsDBNull(0) ? "" : r.GetString(0),
                        Amount = r.IsDBNull(1) ? 0m : Convert.ToDecimal(r.GetValue(1)), // ✅ Net TL
                        Quantity = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2)),
                        Rows = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3))
                    };
                }
            }
            finally
            {
                if (conn.State == ConnectionState.Open) await conn.CloseAsync();
            }
        }

        private async Task FillTopCustomer(SatisAnalizViewModel vm)
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
SELECT TOP 1
    ISNULL(sip_musteri_kod,'') AS CustomerCode,
    SUM({NetTlExpr}) AS AmountNetTL,
    SUM(CAST(ISNULL(sip_miktar,0) AS decimal(18,2))) AS Qty,
    COUNT(1) AS Rows
FROM dbo.SIPARISLER WITH (NOLOCK)
WHERE
    (sip_iptal IS NULL OR sip_iptal = 0)
    AND sip_tarih >= @p1 AND sip_tarih < @p2
GROUP BY ISNULL(sip_musteri_kod,'')
ORDER BY AmountNetTL DESC;
";
                AddDateRangeParams(cmd, vm.StartDate, vm.EndDate);

                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    vm.TopCustomer = new TopCustomer
                    {
                        CustomerCode = r.IsDBNull(0) ? "" : r.GetString(0),
                        Amount = r.IsDBNull(1) ? 0m : Convert.ToDecimal(r.GetValue(1)), // ✅ Net TL
                        Quantity = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2)),
                        Rows = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3))
                    };
                }
            }
            finally
            {
                if (conn.State == ConnectionState.Open) await conn.CloseAsync();
            }
        }

        private async Task FillTopProduct(SatisAnalizViewModel vm)
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
SELECT TOP 1
    ISNULL(sip_stok_kod,'') AS StockCode,
    SUM({NetTlExpr}) AS AmountNetTL,
    SUM(CAST(ISNULL(sip_miktar,0) AS decimal(18,2))) AS Qty,
    COUNT(1) AS Rows
FROM dbo.SIPARISLER WITH (NOLOCK)
WHERE
    (sip_iptal IS NULL OR sip_iptal = 0)
    AND sip_tarih >= @p1 AND sip_tarih < @p2
GROUP BY ISNULL(sip_stok_kod,'')
ORDER BY Qty DESC;
";
                AddDateRangeParams(cmd, vm.StartDate, vm.EndDate);

                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    vm.TopProduct = new TopProduct
                    {
                        StockCode = r.IsDBNull(0) ? "" : r.GetString(0),
                        Amount = r.IsDBNull(1) ? 0m : Convert.ToDecimal(r.GetValue(1)), // ✅ Net TL
                        Quantity = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2)),
                        Rows = r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3))
                    };
                }
            }
            finally
            {
                if (conn.State == ConnectionState.Open) await conn.CloseAsync();
            }
        }

        private async Task FillTopCustomerForTopSeller(SatisAnalizViewModel vm)
        {
            if (vm.TopSeller == null || string.IsNullOrWhiteSpace(vm.TopSeller.SellerCode))
                return;

            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
SELECT TOP 1
    @seller AS SellerCode,
    ISNULL(sip_musteri_kod,'') AS CustomerCode,
    SUM({NetTlExpr}) AS AmountNetTL,
    SUM(CAST(ISNULL(sip_miktar,0) AS decimal(18,2))) AS Qty,
    COUNT(1) AS Rows
FROM dbo.SIPARISLER WITH (NOLOCK)
WHERE
    (sip_iptal IS NULL OR sip_iptal = 0)
    AND ISNULL(sip_satici_kod,'') = @seller
    AND sip_tarih >= @p1 AND sip_tarih < @p2
GROUP BY ISNULL(sip_musteri_kod,'')
ORDER BY AmountNetTL DESC;
";
                AddDateRangeParams(cmd, vm.StartDate, vm.EndDate);

                var pSeller = cmd.CreateParameter();
                pSeller.ParameterName = "@seller";
                pSeller.Value = vm.TopSeller.SellerCode;
                cmd.Parameters.Add(pSeller);

                using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    vm.TopCustomerForTopSeller = new TopCustomerForTopSeller
                    {
                        SellerCode = r.IsDBNull(0) ? "" : r.GetString(0),
                        CustomerCode = r.IsDBNull(1) ? "" : r.GetString(1),
                        Amount = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2)), // ✅ Net TL
                        Quantity = r.IsDBNull(3) ? 0m : Convert.ToDecimal(r.GetValue(3)),
                        Rows = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4))
                    };
                }
            }
            finally
            {
                if (conn.State == ConnectionState.Open) await conn.CloseAsync();
            }
        }

        private async Task FillSellersTopProducts(SatisAnalizViewModel vm)
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
;WITH X AS (
    SELECT
        ISNULL(sip_satici_kod,'') AS SellerCode,
        ISNULL(sip_stok_kod,'') AS StockCode,
        SUM({NetTlExpr}) AS AmountNetTL,
        SUM(CAST(ISNULL(sip_miktar,0) AS decimal(18,2))) AS Qty,
        COUNT(1) AS Rows,
        ROW_NUMBER() OVER (
            PARTITION BY ISNULL(sip_satici_kod,'')
            ORDER BY SUM({NetTlExpr}) DESC
        ) AS rn
    FROM dbo.SIPARISLER WITH (NOLOCK)
    WHERE
        (sip_iptal IS NULL OR sip_iptal = 0)
        AND sip_tarih >= @p1 AND sip_tarih < @p2
    GROUP BY ISNULL(sip_satici_kod,''), ISNULL(sip_stok_kod,'')
)
SELECT SellerCode, StockCode, AmountNetTL, Qty, Rows
FROM X
WHERE rn = 1
ORDER BY AmountNetTL DESC;
";
                AddDateRangeParams(cmd, vm.StartDate, vm.EndDate);

                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    vm.SellersTopProducts.Add(new SellerTopProduct
                    {
                        SellerCode = r.IsDBNull(0) ? "" : r.GetString(0),
                        StockCode = r.IsDBNull(1) ? "" : r.GetString(1),
                        Amount = r.IsDBNull(2) ? 0m : Convert.ToDecimal(r.GetValue(2)), // ✅ Net TL
                        Quantity = r.IsDBNull(3) ? 0m : Convert.ToDecimal(r.GetValue(3)),
                        Rows = r.IsDBNull(4) ? 0 : Convert.ToInt32(r.GetValue(4))
                    });
                }
            }
            finally
            {
                if (conn.State == ConnectionState.Open) await conn.CloseAsync();
            }
        }

        private static void AddDateRangeParams(IDbCommand cmd, DateTime startInclusive, DateTime endInclusive)
        {
            var p1 = cmd.CreateParameter();
            p1.ParameterName = "@p1";
            p1.Value = startInclusive.Date;
            cmd.Parameters.Add(p1);

            var p2 = cmd.CreateParameter();
            p2.ParameterName = "@p2";
            p2.Value = endInclusive.Date.AddDays(1); // [start, end+1)
            cmd.Parameters.Add(p2);
        }
    }
}