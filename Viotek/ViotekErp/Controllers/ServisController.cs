using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ViotekErp.Data;
using ViotekErp.Models;

namespace ViotekErp.Controllers
{
    public class ServisController : Controller
    {
        private readonly MikroDbContext _mikro;
        private readonly ServisDbContext _servisDb;

        public ServisController(MikroDbContext mikro, ServisDbContext servisDb)
        {
            _mikro = mikro;
            _servisDb = servisDb;
        }

        // =========================
        // Helpers (kullanıcı MikroDB'de)
        // =========================
        private string? CurrentUsername()
        {
            return
                User?.FindFirst("KullaniciAdi")?.Value ??
                User?.FindFirst(ClaimTypes.Name)?.Value ??
                User?.Identity?.Name;
        }

        private int? CurrentUserId()
        {
            var idStr = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idStr, out var id) ? id : null;
        }

        private async Task<erp_kullanici?> GetCurrentUserAsync()
        {
            var id = CurrentUserId();
            if (id.HasValue)
            {
                var u = await _mikro.erp_kullanici.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value);
                if (u != null) return u;
            }

            var username = CurrentUsername();
            if (!string.IsNullOrWhiteSpace(username))
            {
                var u = await _mikro.erp_kullanici.AsNoTracking().FirstOrDefaultAsync(x => x.KullaniciAdi == username);
                if (u != null) return u;
            }
            return null;
        }

        private static string NormalizeTab(string? tab)
        {
            tab = (tab ?? "").Trim().ToLowerInvariant();
            if (tab is ServisTabs.Acilan or ServisTabs.Tedarikcide or ServisTabs.TedarikcidenGelen or ServisTabs.TeslimEdilen)
                return tab;

            return ServisTabs.Acilan;
        }

        // =========================
        // PAGE
        // =========================
        [HttpGet]
        public async Task<IActionResult> Index(string tab = ServisTabs.Acilan, string? search = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            tab = NormalizeTab(tab);

            var currentUser = await GetCurrentUserAsync();

            var vm = new ServisIndexVm
            {
                ActiveTab = tab,
                Search = search,
                DateFrom = dateFrom,
                DateTo = dateTo,
                CurrentSipSaticiKod = currentUser?.SipSaticiKod,
                CurrentSipSaticiAd = currentUser?.AdSoyad
            };

            return View(vm);
        }

        // =========================
        // LIST (Servis ViotekAppDB'den; Cari/Stok adı MikroDB'den)
        // =========================
        [HttpGet]
        public async Task<IActionResult> ListPartial(string tab = ServisTabs.Acilan, string? search = null, DateTime? dateFrom = null, DateTime? dateTo = null, int page = 1, int pageSize = 25)
        {
            tab = NormalizeTab(tab);
            if (page < 1) page = 1;
            var allowedPageSizes = new[] { 10, 25, 50, 100 };
            if (!allowedPageSizes.Contains(pageSize)) pageSize = 25;

            var durum = ServisTabs.ToDurum(tab);

            var q = _servisDb.Servisler.AsNoTracking()
                .Where(x => (x.ServisAktif ?? true) == true)
                .Where(x => (x.ServisDurum ?? 1) == durum);

            if (dateFrom.HasValue) q = q.Where(x => x.ServisTarih >= dateFrom.Value.Date);
            if (dateTo.HasValue) q = q.Where(x => x.ServisTarih <= dateTo.Value.Date);

            search = (search ?? "").Trim();

            List<string> matchedCariKodlar = new();
            List<string> matchedStokKodlar = new();

            if (!string.IsNullOrWhiteSpace(search))
            {
                matchedCariKodlar = await _mikro.CariHesaplar.AsNoTracking()
                    .Where(c => (c.Unvan1 ?? "").Contains(search) || (c.CariKod ?? "").Contains(search))
                    .Select(c => c.CariKod).Distinct().Take(500).ToListAsync();

                matchedStokKodlar = await _mikro.Stoklar.AsNoTracking()
                    .Where(s => (s.StoIsim ?? "").Contains(search) || (s.StoKod ?? "").Contains(search))
                    .Select(s => s.StoKod).Distinct().Take(500).ToListAsync();

                q = q.Where(x =>
                    (x.ServisCariKod ?? "").Contains(search) ||
                    (x.ServisStokKod ?? "").Contains(search) ||
                    (x.ServisSeriNumara ?? "").Contains(search) ||
                    (x.ServisArizaAciklama ?? "").Contains(search) ||
                    (!string.IsNullOrWhiteSpace(x.ServisCariKod) && matchedCariKodlar.Contains(x.ServisCariKod)) ||
                    (!string.IsNullOrWhiteSpace(x.ServisStokKod) && matchedStokKodlar.Contains(x.ServisStokKod))
                );
            }

            var totalCount = await q.CountAsync();

            var items = await q
                .OrderByDescending(x => x.ServisTarih)
                .ThenByDescending(x => x.ServisId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // isim sözlükleri MikroDB’den
            var cariKodlar = items.Where(x => !string.IsNullOrWhiteSpace(x.ServisCariKod)).Select(x => x.ServisCariKod!).Distinct().ToList();
            var stokKodlar = items.Where(x => !string.IsNullOrWhiteSpace(x.ServisStokKod)).Select(x => x.ServisStokKod!).Distinct().ToList();

            var cariAdlari = new Dictionary<string, string>();
            if (cariKodlar.Any())
            {
                var cariler = await _mikro.CariHesaplar.AsNoTracking()
                    .Where(c => cariKodlar.Contains(c.CariKod))
                    .Select(c => new { c.CariKod, c.Unvan1 })
                    .ToListAsync();

                cariAdlari = cariler.ToDictionary(x => x.CariKod, x => string.IsNullOrWhiteSpace(x.Unvan1) ? x.CariKod : x.Unvan1!);
            }

            var stokAdlari = new Dictionary<string, string>();
            if (stokKodlar.Any())
            {
                var stoklar = await _mikro.Stoklar.AsNoTracking()
                    .Where(s => stokKodlar.Contains(s.StoKod))
                    .Select(s => new { s.StoKod, s.StoIsim })
                    .ToListAsync();

                stokAdlari = stoklar.ToDictionary(x => x.StoKod, x => string.IsNullOrWhiteSpace(x.StoIsim) ? x.StoKod : x.StoIsim!);
            }

            var vm = new ServisListVm
            {
                ActiveTab = tab,
                Search = search,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Items = items,
                CariAdlari = cariAdlari,
                StokAdlari = stokAdlari,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            return PartialView("_ServisListPartial", vm);
        }
// =========================
// Stage Updates (ViotekAppDB)
// =========================

[HttpPost]
public async Task<IActionResult> SetTedarikciyeGonder(int servisId, DateTime? gonderimTarih, string? tedCariKod, string? aciklama)
{
    var currentUser = await GetCurrentUserAsync();
    if (currentUser == null) return Unauthorized();

    var row = await _servisDb.Servisler.FirstOrDefaultAsync(x => x.ServisId == servisId);
    if (row == null) return NotFound("Servis kaydı bulunamadı.");

    row.TedGonderimTarih = (gonderimTarih ?? DateTime.Today).Date;
    row.TedGonderimCariKod = tedCariKod;
    row.TedGonderimAciklama = aciklama;

    row.ServisDurum = 2;
    row.UpdateId = currentUser.Id;
    row.UpdateDate = DateTime.Now;

    await _servisDb.SaveChangesAsync();
    return Ok(new { ok = true });
}

[HttpPost]
public async Task<IActionResult> SetTedarikcidenGeldi(int servisId, DateTime? alimTarih, string? yapilanIslem, string? yeniSeriNumara, string? teslimAlan)
{
    var currentUser = await GetCurrentUserAsync();
    if (currentUser == null) return Unauthorized();

    var row = await _servisDb.Servisler.FirstOrDefaultAsync(x => x.ServisId == servisId);
    if (row == null) return NotFound("Servis kaydı bulunamadı.");

    row.TedAlimTarih = (alimTarih ?? DateTime.Today).Date;
    row.TedAlimYapilanIslem = yapilanIslem;
    row.TedAlimSeriNumara = yeniSeriNumara;
    row.TedAlimTeslimAlan = teslimAlan;

    row.ServisDurum = 3;
    row.UpdateId = currentUser.Id;
    row.UpdateDate = DateTime.Now;

    await _servisDb.SaveChangesAsync();
    return Ok(new { ok = true });
}

[HttpPost]
public async Task<IActionResult> SetMusteriyeTeslim(int servisId, DateTime? teslimTarih, string? yapilanIslem, string? yeniSeriNumara, string? teslimEden, string? teslimAlan)
{
    var currentUser = await GetCurrentUserAsync();
    if (currentUser == null) return Unauthorized();

    var row = await _servisDb.Servisler.FirstOrDefaultAsync(x => x.ServisId == servisId);
    if (row == null) return NotFound("Servis kaydı bulunamadı.");

    row.MusTeslimTarih = (teslimTarih ?? DateTime.Today).Date;
    row.MusYapilanIslem = yapilanIslem;
    row.MusSeriNumara = yeniSeriNumara;
    row.MusTeslimEden = teslimEden;
    row.MusTeslimAlan = teslimAlan;

    row.ServisDurum = 4;
    row.ServisTamamlandi = true;

    row.UpdateId = currentUser.Id;
    row.UpdateDate = DateTime.Now;

    await _servisDb.SaveChangesAsync();
    return Ok(new { ok = true });
}

[HttpPost]
public async Task<IActionResult> SetAktif(int servisId, bool aktif)
{
    var currentUser = await GetCurrentUserAsync();
    if (currentUser == null) return Unauthorized();

    var row = await _servisDb.Servisler.FirstOrDefaultAsync(x => x.ServisId == servisId);
    if (row == null) return NotFound("Servis kaydı bulunamadı.");

    row.ServisAktif = aktif;
    row.UpdateId = currentUser.Id;
    row.UpdateDate = DateTime.Now;

    await _servisDb.SaveChangesAsync();
    return Ok(new { ok = true });
}
        // =========================
        // MODAL
        // =========================
        [HttpGet]
        public async Task<IActionResult> EditModal(int? id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized("Kullanıcı bulunamadı.");

            TblServis item;

            if (id.HasValue)
            {
                item = await _servisDb.Servisler.FirstOrDefaultAsync(x => x.ServisId == id.Value);
                if (item == null) return NotFound("Servis kaydı bulunamadı.");
            }
            else
            {
                item = new TblServis
                {
                    ServisTarih = DateTime.Today,
                    ServisDurum = 1,
                    ServisTamamlandi = false,
                    ServisAktif = true,
                    ServisGirenSaticiKod = currentUser.SipSaticiKod,
                    ServisTeslimAlan = currentUser.AdSoyad
                };
            }

            string? cariAd = null;
            if (!string.IsNullOrWhiteSpace(item.ServisCariKod))
            {
                cariAd = await _mikro.CariHesaplar.AsNoTracking()
                    .Where(c => c.CariKod == item.ServisCariKod)
                    .Select(c => c.Unvan1)
                    .FirstOrDefaultAsync();
            }

            string? stokAd = null;
            if (!string.IsNullOrWhiteSpace(item.ServisStokKod))
            {
                stokAd = await _mikro.Stoklar.AsNoTracking()
                    .Where(s => s.StoKod == item.ServisStokKod)
                    .Select(s => s.StoIsim)
                    .FirstOrDefaultAsync();
            }

            var vm = new ServisEditVm
            {
                Item = item,
                CariAd = cariAd,
                StokAd = stokAd,
                CurrentSipSaticiKod = currentUser.SipSaticiKod,
                CurrentSipSaticiAd = currentUser.AdSoyad
            };

            return PartialView("_ServisEditModal", vm);
        }

        // =========================
        // SAVE (Servis ViotekAppDB'ye)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save([FromForm] ServisEditVm vm)
        {
            try
            {
                var currentUser = await GetCurrentUserAsync();
                if (currentUser == null) return Unauthorized("Kullanıcı bulunamadı.");

                if (vm?.Item == null)
                    return BadRequest("Form verisi alınamadı (Item boş). Input name'leri Item.* olmalı.");

                if (string.IsNullOrWhiteSpace(vm.Item.ServisCariKod))
                    return BadRequest("Müşteri (Cari) zorunlu.");

                if (string.IsNullOrWhiteSpace(vm.Item.ServisStokKod))
                    return BadRequest("Ürün (Stok) zorunlu.");

                if (vm.Item.ServisId == 0)
                {
                    vm.Item.InsertDate = DateTime.Now;
                    vm.Item.InsertId = currentUser.Id;

                    vm.Item.ServisAktif = true;
                    vm.Item.ServisTamamlandi = false;
                    vm.Item.ServisDurum ??= 1;
                    vm.Item.ServisGirenSaticiKod = currentUser.SipSaticiKod;

                    _servisDb.Servisler.Add(vm.Item);
                }
                else
                {
                    var dbRow = await _servisDb.Servisler.FirstOrDefaultAsync(x => x.ServisId == vm.Item.ServisId);
                    if (dbRow == null) return NotFound("Servis kaydı bulunamadı.");

                    dbRow.ServisTarih = vm.Item.ServisTarih;
                    dbRow.ServisCariKod = vm.Item.ServisCariKod;
                    dbRow.ServisStokKod = vm.Item.ServisStokKod;
                    dbRow.ServisSeriNumara = vm.Item.ServisSeriNumara;
                    dbRow.ServisSatinalmaTarih = vm.Item.ServisSatinalmaTarih;
                    dbRow.ServisGaranti = vm.Item.ServisGaranti;
                    dbRow.ServisTeslimEden = vm.Item.ServisTeslimEden;
                    dbRow.ServisTeslimAlan = vm.Item.ServisTeslimAlan;
                    dbRow.ServisArizaAciklama = vm.Item.ServisArizaAciklama;

                    if (string.IsNullOrWhiteSpace(dbRow.ServisGirenSaticiKod))
                        dbRow.ServisGirenSaticiKod = currentUser.SipSaticiKod;

                    dbRow.UpdateDate = DateTime.Now;
                    dbRow.UpdateId = currentUser.Id;
                }

                await _servisDb.SaveChangesAsync();
                return Ok(new { ok = true });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "DB hata: " + (ex.InnerException?.Message ?? ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Sunucu hata: " + ex.Message);
            }
        }

        // =========================
        // Suggest (MikroDB)
        // =========================
        [HttpGet]
        public async Task<IActionResult> CariSuggest(string? q)
        {
            q = (q ?? "").Trim();
            if (q.Length < 2) return Json(Array.Empty<object>());

            var list = await _mikro.CariHesaplar.AsNoTracking()
                .Where(c => (c.CariKod ?? "").Contains(q) || (c.Unvan1 ?? "").Contains(q))
                .OrderBy(c => c.CariKod)
                .Select(c => new { kod = c.CariKod, ad = c.Unvan1 })
                .Take(10)
                .ToListAsync();

            return Json(list);
        }

        [HttpGet]
        public async Task<IActionResult> StokSuggest(string? q)
        {
            q = (q ?? "").Trim();
            if (q.Length < 2) return Json(Array.Empty<object>());

            var list = await _mikro.Stoklar.AsNoTracking()
                .Where(s => (s.StoKod ?? "").Contains(q) || (s.StoIsim ?? "").Contains(q))
                .OrderBy(s => s.StoKod)
                .Select(s => new { kod = s.StoKod, ad = s.StoIsim })
                .Take(10)
                .ToListAsync();

            return Json(list);
        }
        [HttpGet]
        public async Task<IActionResult> TedarikciSuggest(string? q)
        {
            q = (q ?? "").Trim();
            if (q.Length < 2) return Json(Array.Empty<object>());

            var list = await _mikro.CariHesaplar.AsNoTracking()
                .Where(c =>
                    (c.CariKod ?? "").Contains(q) ||
                    (c.Unvan1 ?? "").Contains(q))
                .OrderBy(c => c.CariKod)
                .Select(c => new { kod = c.CariKod, ad = c.Unvan1 })
                .Take(10)
                .ToListAsync();

            return Json(list);
        }
    }
}