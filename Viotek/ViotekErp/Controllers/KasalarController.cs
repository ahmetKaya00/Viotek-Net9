using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViotekErp.Data;
using ViotekErp.Models;

namespace ViotekErp.Controllers
{
    public class KasalarController : Controller
    {
        private readonly MikroDbContext _db;

        public KasalarController(MikroDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? search = null)
        {
            var q = _db.KasalarYonetim.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x =>
                    (x.KasaKodu ?? "").Contains(s) ||
                    (x.KasaAdi ?? "").Contains(s) ||
                    (x.Doviz ?? "").Contains(s));
            }

            var items = await q
                .OrderBy(x => x.KasaKodu)
                .ToListAsync();

            var vm = new KasalarViewModel
            {
                Search = search,
                Items = items,
                ToplamAna = items.Sum(x => x.AnaDovizBakiye ?? 0),
ToplamOrjinal = items.Sum(x => x.OrjinalDovizBakiye ?? 0),
            };

            return View(vm);
        }
    }
}