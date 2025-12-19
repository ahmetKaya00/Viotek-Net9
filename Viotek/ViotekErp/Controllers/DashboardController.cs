using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViotekErp.Data;
using ViotekErp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ViotekErp.Controllers
{
    public class DashboardController : Controller
    {
        private readonly MikroDbContext _db;

        public DashboardController(MikroDbContext db)
        {
            _db = db;
        }

        // Controller içinde hesap için concrete tip (dynamic yok -> CS1977 yok)
        private sealed class SiparisCalcRow
        {
            public Guid SipGuid { get; set; }
            public DateTime? SipTarih { get; set; }

            public string? MusteriKod { get; set; }
            public string? SaticiKod { get; set; }

            public string? EvrakSeri { get; set; }
            public int? EvrakSira { get; set; }

            public string? StokKod { get; set; }
            public double? Miktar { get; set; }

            public double? Tutar { get; set; }     // sip_tutar (döviz / TL)
            public byte? DovizCinsi { get; set; }  // sip_doviz_cinsi
            public double? DovizKuru { get; set; } // sip_doviz_kuru

            public double? Iskonto1 { get; set; }  // sip_iskonto_1 (TUTAR)
            public double? Iskonto2 { get; set; }
            public double? Iskonto3 { get; set; }
            public double? Iskonto4 { get; set; }
            public double? Iskonto5 { get; set; }
            public double? Iskonto6 { get; set; }
        }

        public async Task<IActionResult> Index(
            string period = "month",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var model = new DashboardViewModel
            {
                Period = string.IsNullOrWhiteSpace(period) ? "month" : period.Trim().ToLowerInvariant(),
                FilterStartDate = startDate,
                FilterEndDate = endDate
            };

            // İptal olmayan ve tarihi dolu sipariş satırları
            var baseQuery = _db.Siparisler
                .AsNoTracking()
                .Where(s => s.SipTarih.HasValue && (s.Iptal == null || s.Iptal == false));

            // Veride hiç sipariş yoksa
            var lastDateInData = await baseQuery.MaxAsync(s => s.SipTarih);
            if (lastDateInData == null)
                return View(model);

            // ✅ Referans tarih: VERİDEKİ EN SON GÜN (bugün değil!)
            var refDate = DateTime.Today;
            // ✅ Filtre aralığı belirle (inclusive)
            DateTime rangeStart;
            DateTime rangeEnd;

            switch (model.Period)
            {
                case "day":
                    rangeStart = refDate;
                    rangeEnd = refDate;
                    break;

                case "week":
                    rangeStart = refDate.AddDays(-6);
                    rangeEnd = refDate;
                    break;

                case "year":
                    rangeStart = new DateTime(refDate.Year, 1, 1);
                    rangeEnd = new DateTime(refDate.Year, 12, 31);
                    break;

                case "custom":
                    rangeEnd = (endDate ?? refDate).Date;
                    rangeStart = (startDate ?? rangeEnd.AddDays(-29)).Date;
                    if (rangeStart > rangeEnd)
                    {
                        var tmp = rangeStart;
                        rangeStart = rangeEnd;
                        rangeEnd = tmp;
                    }
                    break;

                case "month":
                default:
                    rangeStart = new DateTime(refDate.Year, refDate.Month, 1);
                    rangeEnd = rangeStart.AddMonths(1).AddDays(-1);
                    break;
            }

            model.FilterStartDate = rangeStart;
            model.FilterEndDate = rangeEnd;

            // ✅ İhtiyaç duyduğumuz alanları çek
            var allRows = await baseQuery
                .Select(s => new SiparisCalcRow
                {
                    SipGuid = s.SipGuid,
                    SipTarih = s.SipTarih,

                    MusteriKod = s.MusteriKod,
                    SaticiKod = s.SaticiKod,

                    EvrakSeri = s.EvrakSeri,
                    EvrakSira = s.EvrakSira,

                    StokKod = s.StokKod,
                    Miktar = s.Miktar,

                    Tutar = s.Tutar,
                    DovizCinsi = s.DovizCinsi,
                    DovizKuru = s.DovizKuru,

                    Iskonto1 = s.Iskonto1,
                    Iskonto2 = s.Iskonto2,
                    Iskonto3 = s.Iskonto3,
                    Iskonto4 = s.Iskonto4,
                    Iskonto5 = s.Iskonto5,
                    Iskonto6 = s.Iskonto6
                })
                .ToListAsync();

            // ✅ Mikro mantığı: sip_tutar - (sip_iskonto_1..6) = net döviz, sonra kur ile TL
            double NetTutarTL(SiparisCalcRow x)
            {
                double brut = x.Tutar ?? 0;

                double iskTutar =
                    (x.Iskonto1 ?? 0) +
                    (x.Iskonto2 ?? 0) +
                    (x.Iskonto3 ?? 0) +
                    (x.Iskonto4 ?? 0) +
                    (x.Iskonto5 ?? 0) +
                    (x.Iskonto6 ?? 0);

                double netDoviz = brut - iskTutar;

                byte dovizCinsi = x.DovizCinsi ?? (byte)0;
                double kur = (dovizCinsi == 0) ? 1.0 : (x.DovizKuru ?? 1.0);

                return netDoviz * kur;
            }

            // --------------------------
            // GENEL (tüm zamanlar)
            // --------------------------
            model.TotalOrderCount = allRows.Count;
            model.TotalSalesAllTime = allRows.Sum(NetTutarTL);

            model.FirstOrderDate = allRows
                .OrderBy(x => x.SipTarih)
                .Select(x => x.SipTarih)
                .FirstOrDefault();

            model.LastOrderDate = lastDateInData;

            // --------------------------
            // Seçilen aralık
            // --------------------------
            var rangeRows = allRows
                .Where(s => s.SipTarih!.Value.Date >= rangeStart && s.SipTarih!.Value.Date <= rangeEnd)
                .ToList();

            // Kart: Toplam satış (seçilen dönem)
            model.MonthlySalesTotal = rangeRows.Sum(NetTutarTL);

            // Kart: Bugünkü sipariş sayısı (refDate günü)
            // İstersen "evrak bazında" sayalım:
            model.TodayOrderCount = allRows
                .Where(s => s.SipTarih!.Value.Date == refDate)
                .Where(s => !string.IsNullOrEmpty(s.EvrakSeri) && s.EvrakSira.HasValue)
                .GroupBy(s => new { s.EvrakSeri, s.EvrakSira })
                .Count();

            // Seçilen dönem personeli (satıcı)
            var bestSeller = rangeRows
                .Where(s => !string.IsNullOrEmpty(s.SaticiKod))
                .GroupBy(s => s.SaticiKod!)
                .Select(g => new
                {
                    SaticiKod = g.Key,
                    Total = g.Sum(NetTutarTL)
                })
                .OrderByDescending(x => x.Total)
                .FirstOrDefault();

            if (bestSeller != null)
            {
                model.EmployeeOfMonthSales = bestSeller.Total;

                var satici = await _db.SorumluAdlari
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SaticiKod == bestSeller.SaticiKod);

                model.EmployeeOfMonthName =
                    (!string.IsNullOrWhiteSpace(satici?.SaticiAd))
                        ? satici!.SaticiAd
                        : bestSeller.SaticiKod;
            }

            // --------------------------
            // Grafik: seçilen aralığın son günlerinden 7 günlük seri
            // --------------------------
            var chartEnd = rangeEnd.Date;
            var chartStart = chartEnd.AddDays(-6);
            if (chartStart < rangeStart.Date) chartStart = rangeStart.Date;

            var chartRows = rangeRows
                .Where(s => s.SipTarih!.Value.Date >= chartStart && s.SipTarih!.Value.Date <= chartEnd)
                .ToList();

            var chartData = chartRows
                .GroupBy(s => s.SipTarih!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(NetTutarTL)
                })
                .ToList();

            var points = new List<WeeklySalesPoint>();
            var dayCount = (int)(chartEnd - chartStart).TotalDays + 1;

            for (int i = 0; i < dayCount; i++)
            {
                var d = chartStart.AddDays(i).Date;
                var row = chartData.FirstOrDefault(x => x.Date == d);

                points.Add(new WeeklySalesPoint
                {
                    DayLabel = d.ToString("dd.MM"),
                    Amount = row?.Amount ?? 0
                });
            }

// --------------------------
// GRAFİK: period’a göre kırılım
// year  -> aylık
// month -> günlük
// week  -> günlük
// day   -> saatlik (SipTarih saat içeriyorsa)
// custom-> aralığa göre (basit: 60 günden uzunsa aylık, değilse günlük)
// --------------------------
List<WeeklySalesPoint> chartPoints;

string p = (model.Period ?? "month").ToLowerInvariant();

bool IsCustomLongRange()
{
    var days = (rangeEnd.Date - rangeStart.Date).TotalDays + 1;
    return days > 60;
}

if (p == "year")
{
    // 12 ay
    var grouped = rangeRows
        .GroupBy(x => new { x.SipTarih!.Value.Year, x.SipTarih!.Value.Month })
        .Select(g => new
        {
            g.Key.Year,
            g.Key.Month,
            Amount = g.Sum(NetTutarTL)
        })
        .ToList();

    chartPoints = new List<WeeklySalesPoint>();
    for (int m = 1; m <= 12; m++)
    {
        var row = grouped.FirstOrDefault(x => x.Year == rangeStart.Year && x.Month == m);
        chartPoints.Add(new WeeklySalesPoint
        {
            DayLabel = $"{m:00}.{rangeStart.Year}",   // 01.2025
            Amount = row?.Amount ?? 0
        });
    }
}
else if (p == "day")
{
    // Saatlik 00..23  (⚠️ SipTarih’te saat yoksa hepsi 00’a yığılır)
    var grouped = rangeRows
        .GroupBy(x => x.SipTarih!.Value.Hour)
        .Select(g => new
        {
            Hour = g.Key,
            Amount = g.Sum(NetTutarTL)
        })
        .ToList();

    chartPoints = new List<WeeklySalesPoint>();
    for (int h = 0; h < 24; h++)
    {
        var row = grouped.FirstOrDefault(x => x.Hour == h);
        chartPoints.Add(new WeeklySalesPoint
        {
            DayLabel = $"{h:00}:00",
            Amount = row?.Amount ?? 0
        });
    }
}
else
{
    // week / month / custom
    bool useMonthly = (p == "custom" && IsCustomLongRange());

    if (useMonthly)
    {
        // custom uzun aralık: aylık
        var grouped = rangeRows
            .GroupBy(x => new { x.SipTarih!.Value.Year, x.SipTarih!.Value.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Amount = g.Sum(NetTutarTL)
            })
            .ToList();

        chartPoints = new List<WeeklySalesPoint>();

        // rangeStart-rangeEnd arasındaki ayları sırayla üret
        var cursor = new DateTime(rangeStart.Year, rangeStart.Month, 1);
        var endCursor = new DateTime(rangeEnd.Year, rangeEnd.Month, 1);

        while (cursor <= endCursor)
        {
            var y = cursor.Year;
            var m = cursor.Month;

            var row = grouped.FirstOrDefault(x => x.Year == y && x.Month == m);

            chartPoints.Add(new WeeklySalesPoint
            {
                DayLabel = $"{m:00}.{y}",
                Amount = row?.Amount ?? 0
            });

            cursor = cursor.AddMonths(1);
        }
    }
    else
    {
        // günlük: rangeStart..rangeEnd (week ise zaten 7 gün)
        var grouped = rangeRows
            .GroupBy(x => x.SipTarih!.Value.Date)
            .Select(g => new
            {
                Date = g.Key,
                Amount = g.Sum(NetTutarTL)
            })
            .ToList();

        chartPoints = new List<WeeklySalesPoint>();

        for (var d = rangeStart.Date; d <= rangeEnd.Date; d = d.AddDays(1))
        {
            var row = grouped.FirstOrDefault(x => x.Date == d);

            chartPoints.Add(new WeeklySalesPoint
            {
                DayLabel = d.ToString("dd.MM"),
                Amount = row?.Amount ?? 0
            });
        }
    }
}

model.ChartPoints = chartPoints;

// İstersen WeeklySales’i de aynı şeye bağla (View eskiyse kırılmasın)
model.WeeklySales = model.ChartPoints;
            // --------------------------
            // Son 30 gün (refDate’e göre)
            // --------------------------
            var last30Start = refDate.AddDays(-29);
            var last30Rows = allRows
                .Where(s => s.SipTarih!.Value.Date >= last30Start && s.SipTarih!.Value.Date <= refDate)
                .ToList();

            model.Last30DaysSalesTotal = last30Rows.Sum(NetTutarTL);
            model.Last30DaysOrderCount = last30Rows
                .Where(s => !string.IsNullOrEmpty(s.EvrakSeri) && s.EvrakSira.HasValue)
                .GroupBy(s => new { s.EvrakSeri, s.EvrakSira })
                .Count();

            // --------------------------
            // ✅ Son 10 SİPARİŞ (EVRAK bazında - tek satır)
            // --------------------------
            model.LastOrders = allRows
                .Where(s => !string.IsNullOrEmpty(s.EvrakSeri) && s.EvrakSira.HasValue)
                .GroupBy(s => new { s.EvrakSeri, s.EvrakSira })
                .Select(g => new DashboardLastOrderDto
                {
                    EvrakSeri = g.Key.EvrakSeri,
                    EvrakSira = g.Key.EvrakSira,

                    SipTarih = g.Max(x => x.SipTarih),
                    MusteriKod = g.Select(x => x.MusteriKod).FirstOrDefault(),
                    SaticiKod = g.Select(x => x.SaticiKod).FirstOrDefault(),

                    KalemSayisi = g.Count(),
                    ToplamMiktar = g.Sum(x => x.Miktar ?? 0),

                    Tutar = g.Sum(NetTutarTL),
                    TutarKdvliTL = g.Sum(NetTutarTL) * 1.20
                })
                .OrderByDescending(x => x.SipTarih)
                .ThenByDescending(x => x.EvrakSira)
                .Take(10)
                .ToList();

            // Müşteri adları
            var lastMusteriKodlar = model.LastOrders
                .Where(x => !string.IsNullOrEmpty(x.MusteriKod))
                .Select(x => x.MusteriKod!)
                .Distinct()
                .ToList();

            if (lastMusteriKodlar.Any())
            {
                var cariList = await _db.CariHesaplar
                    .AsNoTracking()
                    .Where(c => lastMusteriKodlar.Contains(c.CariKod))
                    .ToListAsync();

                model.LastOrdersMusteriAdlari = cariList
                    .ToDictionary(c => c.CariKod, c => c.Unvan1 ?? c.CariKod);
            }

            // Satıcı adları
            var lastSaticiKodlar = model.LastOrders
                .Where(x => !string.IsNullOrEmpty(x.SaticiKod))
                .Select(x => x.SaticiKod!)
                .Distinct()
                .ToList();

            if (lastSaticiKodlar.Any())
            {
                var saticiList = await _db.SorumluAdlari
                    .AsNoTracking()
                    .Where(s => lastSaticiKodlar.Contains(s.SaticiKod))
                    .ToListAsync();

                model.LastOrdersSaticiAdlari = saticiList
                    .ToDictionary(
                        s => s.SaticiKod,
                        s => string.IsNullOrWhiteSpace(s.SaticiAd) ? s.SaticiKod : s.SaticiAd
                    );
            }

            return View(model);
        }

        public IActionResult Finance()
        {
            return RedirectToAction("Index", "Finance");
        }
    }
}