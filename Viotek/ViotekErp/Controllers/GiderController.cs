using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using ViotekErp.Data;
using ViotekErp.Models;

namespace ViotekErp.Controllers
{
    public class GiderController : Controller
    {
        private readonly MikroDbContext _db;

        public GiderController(MikroDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string period = "month",
            string? month = null,
            string? search = null,
            string sort = "tarih_desc",
            int page = 1,
            int pageSize = 25)
        {
            var allowed = new[] { 10, 25, 50, 100 };
            if (!allowed.Contains(pageSize)) pageSize = 25;
            if (page < 1) page = 1;

            period = (period ?? "month").Trim().ToLowerInvariant();
            if (period != "all" && period != "month") period = "month";

            // Baz query
            var q = _db.GiderHareketler
                .AsNoTracking()
                .AsQueryable();

            // -------------------------
            // ✅ Tarih aralığı belirle
            // -------------------------
            DateTime? start = null;
            DateTime? end = null; // inclusive

            if (period == "month")
            {
                // ay yoksa: verideki son tarihin ayı
                DateTime refDate;
                var maxDate = await q.MaxAsync(x => x.MsgS0089);
                refDate = (maxDate ?? DateTime.Today).Date;

                if (!string.IsNullOrWhiteSpace(month) &&
                    DateTime.TryParseExact(month + "-01", "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    refDate = parsed.Date;
                }
                else
                {
                    month = refDate.ToString("yyyy-MM");
                }

                start = new DateTime(refDate.Year, refDate.Month, 1);
                end = start.Value.AddMonths(1).AddDays(-1);
            }

            // Tarih filtresi uygula
            if (start.HasValue && end.HasValue)
            {
                var s = start.Value.Date;
                var e = end.Value.Date;
                q = q.Where(x => x.MsgS0089.HasValue &&
                                 x.MsgS0089.Value.Date >= s &&
                                 x.MsgS0089.Value.Date <= e);
            }

            // -------------------------
            // ✅ Arama
            // -------------------------
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                if (Guid.TryParse(search, out var gid))
                {
                    q = q.Where(x => x.MsgS0088 == gid);
                }
                else if (DateTime.TryParse(search, out var dt))
                {
                    var d = dt.Date;
                    q = q.Where(x => x.MsgS0089.HasValue && x.MsgS0089.Value.Date == d);
                }
                else
                {
                    q = q.Where(x =>
                        (x.MsgS0223 != null && x.MsgS0223.Contains(search)) ||
                        (x.MsgS0433 != null && x.MsgS0433.Contains(search)) ||
                        (x.MsgS1167 != null && x.MsgS1167.Contains(search)) ||
                        (x.MsgS1168 != null && x.MsgS1168.Contains(search)) ||
                        (x.MsgS1160 != null && x.MsgS1160.Contains(search))
                    );
                }
            }

            // -------------------------
            // ✅ Özetler (filtreli)
            // -------------------------
            var totalCount = await q.CountAsync();

            var totalNet = await q.SumAsync(x => (double?)(x.MsgS1164 ?? 0)) ?? 0;
            var totalAbs = await q.SumAsync(x => (double?)(Math.Abs(x.MsgS1164 ?? 0))) ?? 0;
            var negCount = await q.CountAsync(x => (x.MsgS1164 ?? 0) < 0);
            var posCount = await q.CountAsync(x => (x.MsgS1164 ?? 0) > 0);

            // -------------------------
            // ✅ Pasta: en büyük 8 + diğer
            // -------------------------
            var rawPie = await q
                .Where(x => x.MsgS1168 != null && x.MsgS1168 != "")
                .GroupBy(x => x.MsgS1168!)
                .Select(g => new
                {
                    Name = g.Key,
                    SumAbs = g.Sum(x => Math.Abs(x.MsgS1164 ?? 0))
                })
                .OrderByDescending(x => x.SumAbs)
                .ToListAsync();

            var topN = 8;
            var top = rawPie.Take(topN).ToList();
            var otherSum = rawPie.Skip(topN).Sum(x => x.SumAbs);

            var pie = top.Select(x => new PiePoint { Label = x.Name, Value = x.SumAbs }).ToList();
            if (otherSum > 0) pie.Add(new PiePoint { Label = "Diğer", Value = otherSum });

            // -------------------------
            // ✅ Grafik: günlük mutlak toplam
            // -------------------------
            var daily = await q
                .Where(x => x.MsgS0089.HasValue)
                .GroupBy(x => x.MsgS0089!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    SumAbs = g.Sum(x => Math.Abs(x.MsgS1164 ?? 0))
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var dailySeries = daily.Select(x => new SeriesPoint
            {
                Label = x.Date.ToString("dd.MM"),
                Value = x.SumAbs
            }).ToList();

            // -------------------------
            // ✅ Pagination + List
            // -------------------------
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            q = sort == "tarih_asc"
                ? q.OrderBy(x => x.MsgS0089).ThenBy(x => x.MsgS0088)
                : q.OrderByDescending(x => x.MsgS0089).ThenByDescending(x => x.MsgS0088);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new GiderListItemDto
                {
                    MsgS0088 = x.MsgS0088,
                    MsgS0089 = x.MsgS0089,
                    MsgS0223 = x.MsgS0223,
                    MsgS0433 = x.MsgS0433,
                    MsgS1167 = x.MsgS1167,
                    MsgS1168 = x.MsgS1168,
                    MsgS1164 = x.MsgS1164,
                    MsgS1160 = x.MsgS1160,
                    MsgS1165 = x.MsgS1165
                })
                .ToListAsync();

            var model = new GiderListViewModel
            {
                Period = period,
                Month = month,
                StartDate = start,
                EndDate = end,

                Items = items,
                Search = search,
                Sort = string.IsNullOrWhiteSpace(sort) ? "tarih_desc" : sort,
                Page = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCount = totalCount,

                TotalAmountNet = totalNet,
                TotalAmountAbs = totalAbs,
                NegativeCount = negCount,
                PositiveCount = posCount,

                PieByExpenseName = pie,
                DailyTotalsAbs = dailySeries
            };

            return View(model);
        }
    }
}