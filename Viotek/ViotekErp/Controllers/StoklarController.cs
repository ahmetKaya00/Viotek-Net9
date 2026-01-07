using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ViotekErp.Data;
using ViotekErp.Models;

namespace ViotekErp.Controllers
{
    public class StoklarController : Controller
    {
        private readonly MikroDbContext _db;

        public StoklarController(MikroDbContext db)
        {
            _db = db;
        }

        // ----------------------------------------------------------
        //  LISTE
        // ----------------------------------------------------------
        public async Task<IActionResult> Index(
            string? search,
            double? minMiktar,
            int page = 1,
            int pageSize = 10)
        {
            var allowedPageSizes = new[] { 10, 25, 50 };
            if (!allowedPageSizes.Contains(pageSize))
                pageSize = 10;

            // VW_STOK_MEVCUT + STOKLAR join
            var query =
                from m in _db.StokMevcut
                join s in _db.Stoklar on m.StoKod equals s.StoKod
                select new { m, s };

            // Filtreler
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    x.m.StoKod.Contains(search) ||
                    (x.m.StoIsim != null && x.m.StoIsim.Contains(search)) ||
                    (x.s.MarkaKodu != null && x.s.MarkaKodu.Contains(search)) ||
                    (x.s.KategoriKodu != null && x.s.KategoriKodu.Contains(search)));
            }

            if (minMiktar.HasValue)
            {
                query = query.Where(x => x.m.MevcutMiktar >= minMiktar.Value);
            }

            // Toplam count & toplam stok
            var totalCount = await query.CountAsync();
            var totalMevcut = await query.SumAsync(x => (double?)x.m.MevcutMiktar) ?? 0;

            if (page < 1) page = 1;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var list = await query
                .OrderByDescending(x => x.m.MevcutMiktar)
                .ThenBy(x => x.m.StoKod)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Sayfada görünen stok kodları
            var stokKodList = list.Select(x => x.m.StoKod).Distinct().ToList();

            // ----------------------------------------------------------
            // ✅ 1) Fiyat: STOKDETAY'dan toplu çek
            // ----------------------------------------------------------
            var fiyatRows = await _db.StokDetay
                .AsNoTracking()
                .Where(x => stokKodList.Contains(x.StokKodu))
                .Select(x => new { x.StokKodu, x.Fiyat, x.FiyatDoviz })
                .ToListAsync();

            var fiyatMap = fiyatRows.ToDictionary(x => x.StokKodu, x => x);

            // ----------------------------------------------------------
            // ✅ 2) Son hareket tarihi: STOK_HAREKETLERI'nden toplu çek
            // (Fiyat değil, sadece tarih için)
            // ----------------------------------------------------------
            var sonHareketTarihleri = await _db.StokHareketleri
                .AsNoTracking()
                .Where(h => h.StokKod != null && stokKodList.Contains(h.StokKod))
                .GroupBy(h => h.StokKod!)
                .Select(g => new
                {
                    StokKod = g.Key,
                    SonTarih = g.Max(x => x.SthTarih)
                })
                .ToListAsync();

            var tarihMap = sonHareketTarihleri.ToDictionary(x => x.StokKod, x => x.SonTarih);

            var items = new List<StokListItemDto>();

            foreach (var row in list)
            {
                fiyatMap.TryGetValue(row.m.StoKod, out var fInfo);
                tarihMap.TryGetValue(row.m.StoKod, out var sonTarih);

                items.Add(new StokListItemDto
                {
                    StokKod = row.m.StoKod,
                    StokIsim = row.m.StoIsim ?? "",
                    MevcutMiktar = row.m.MevcutMiktar,
                    MarkaKodu = row.s.MarkaKodu,
                    KategoriKodu = row.s.KategoriKodu,

                    // ✅ Tarih hareketten
                    SonHareketTarihi = sonTarih,

                    // ✅ Fiyat STOKDETAY'dan
                    SonSatisFiyati = fInfo?.Fiyat,
                    StandartMaliyet = row.s.StandartMaliyet,
                });
            }

            var model = new StokListViewModel
            {
                Items = items,
                Search = search,
                MinMiktar = minMiktar,
                Page = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalMevcut = totalMevcut
            };

            return View(model);
        }

        // ----------------------------------------------------------
        //  DETAY POPUP (PARTIAL)
        // ----------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> DetayPartial(string stokKod)
        {
            if (string.IsNullOrWhiteSpace(stokKod))
                return BadRequest("Stok kodu boş.");

            // Stok + view’den mevcut / marka / kategori
            var stokRow =
                await (from m in _db.StokMevcut
                       join s in _db.Stoklar on m.StoKod equals s.StoKod
                       where m.StoKod == stokKod
                       select new { m, s })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (stokRow == null)
                return NotFound("Stok bulunamadı.");

            // ----------------------------------------------------------
            // ✅ Fiyat: STOKDETAY'dan çek (msg_S_0006)
            // ----------------------------------------------------------
            var fiyatDetay = await _db.StokDetay
                .AsNoTracking()
                .Where(x => x.StokKodu == stokKod)
                .Select(x => (double?)x.Fiyat)
                .FirstOrDefaultAsync();

            // Hareketler (son 50)
            var hareketler = await _db.StokHareketleri
                .AsNoTracking()
                .Where(h => h.StokKod == stokKod)
                .OrderByDescending(h => h.SthTarih)
                .ThenByDescending(h => h.SthGuid)
                .Take(50)
                .ToListAsync();

            DateTime? sonTarih = null;
            if (hareketler.Any())
            {
                sonTarih = hareketler.Max(x => x.SthTarih);
            }

            // ----------------------------------------------------------
            // ✅ CARİ ADLARI (CariHesaplar: CariKod -> Unvan1)
            // ----------------------------------------------------------
            var cariKodlar = hareketler
                .Where(x => !string.IsNullOrWhiteSpace(x.CariKodu))
                .Select(x => x.CariKodu!)
                .Distinct()
                .ToList();

            var cariAdlari = new Dictionary<string, string>();

            if (cariKodlar.Any())
            {
                var cariList = await _db.CariHesaplar
                    .AsNoTracking()
                    .Where(c => cariKodlar.Contains(c.CariKod))
                    .ToListAsync();

                cariAdlari = cariList
                    .ToDictionary(c => c.CariKod, c => c.Unvan1 ?? c.CariKod);
            }

            // ----------------------------------------------------------
            // ✅ PLASİYER ADLARI (SATICILAR_VIEW: SaticiKod -> SaticiAd)
            // ----------------------------------------------------------
            var plasiyerKodlar = hareketler
                .Where(x => !string.IsNullOrWhiteSpace(x.PlasiyerKodu))
                .Select(x => x.PlasiyerKodu!)
                .Distinct()
                .ToList();

            var plasiyerAdlari = new Dictionary<string, string>();

            if (plasiyerKodlar.Any())
            {
                var plasiyerList = await _db.SorumluAdlari
                    .AsNoTracking()
                    .Where(x => plasiyerKodlar.Contains(x.SaticiKod))
                    .ToListAsync();

                plasiyerAdlari = plasiyerList
                    .ToDictionary(
                        x => x.SaticiKod,
                        x => string.IsNullOrWhiteSpace(x.SaticiAd) ? x.SaticiKod : x.SaticiAd
                    );
            }

            // ----------------------------------------------------------
            // DTO
            // ----------------------------------------------------------
            var dtos = hareketler.Select(h =>
            {
                string tipAciklama = "Hareket";
                if (h.Tip.HasValue)
                {
                    tipAciklama = h.Tip switch
                    {
                        0 => "Giriş",
                        1 => "Çıkış",
                        2 => "İade / Giriş",
                        3 => "Transfer / Giriş",
                        4 => "Transfer / Çıkış",
                        _ => $"Tip {h.Tip}"
                    };
                }

                // ✅ isimleri çöz (yoksa koda düş)
                string? cariAd = null;
                if (!string.IsNullOrWhiteSpace(h.CariKodu) &&
                    cariAdlari.TryGetValue(h.CariKodu!, out var ca))
                    cariAd = ca;

                string? plasiyerAd = null;
                if (!string.IsNullOrWhiteSpace(h.PlasiyerKodu) &&
                    plasiyerAdlari.TryGetValue(h.PlasiyerKodu!, out var pa))
                    plasiyerAd = pa;

                return new StokHareketDetayDto
                {
                    Tarih = h.SthTarih,
                    EvrakNo = $"{h.EvrakSeri} {h.EvrakSira}",
                    TipAciklama = tipAciklama,
                    Miktar = h.Miktar,
                    Tutar = h.Tutar,

                    CariKodu = h.CariKodu,
                    CariAd = cariAd,

                    PlasiyerKodu = h.PlasiyerKodu,
                    PlasiyerAd = plasiyerAd,

                    GirisDepoNo = h.GirisDepoNo,
                    CikisDepoNo = h.CikisDepoNo
                };
            }).ToList();

            var model = new StokDetayViewModel
            {
                StokKod = stokRow.m.StoKod,
                StokIsim = stokRow.m.StoIsim,
                MevcutMiktar = stokRow.m.MevcutMiktar,
                MarkaKodu = stokRow.s.MarkaKodu,
                KategoriKodu = stokRow.s.KategoriKodu,
                SonHareketTarihi = sonTarih,

                // ✅ Fiyat: STOKDETAY
                SonSatisFiyati = fiyatDetay,
                StandartMaliyet = stokRow.s.StandartMaliyet,

                Hareketler = dtos
            };

            return PartialView("_DetayPartial", model);
        }
    }
}