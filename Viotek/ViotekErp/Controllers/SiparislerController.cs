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
    public class SiparislerController : Controller
    {
        private readonly MikroDbContext _db;

        public SiparislerController(MikroDbContext db)
        {
            _db = db;
        }

        private static readonly Dictionary<byte, string> DovizSozluk = new()
        {
            { 0, "TL" },
            { 1, "USD" },
            { 2, "EUR" },
            { 3, "GBP" }
        };

        // ✅ İskonto kolonları sende TUTAR olduğu için:
        // brüt = birim * miktar
        // netDoviz = brüt - iskonto1 - iskonto2 - ...
        private static double HesaplaNetTutarDoviz(Siparis x)
        {
            var miktar = x.Miktar ?? 0;
            var birim = x.BirimFiyat ?? 0;

            var brut = birim * miktar;

            var net = brut
                      - (x.Iskonto1 ?? 0)
                      - (x.Iskonto2 ?? 0)
                      - (x.Iskonto3 ?? 0)
                      - (x.Iskonto4 ?? 0)
                      - (x.Iskonto5 ?? 0)
                      - (x.Iskonto6 ?? 0);

            return net;
        }

        private static double HesaplaNetTutarTL(Siparis x)
        {
            var netDoviz = HesaplaNetTutarDoviz(x);

            var dovizCinsi = x.DovizCinsi ?? (byte)0;
            var kur = (dovizCinsi == 0) ? 1.0 : (x.DovizKuru ?? 1.0);

            return netDoviz * kur;
        }

        private static double HesaplaIskontoYuzde(Siparis x)
        {
            var miktar = x.Miktar ?? 0;
            var birim = x.BirimFiyat ?? 0;
            var brut = birim * miktar;
            if (brut == 0) return 0;

            var toplamIskontoTutar =
                (x.Iskonto1 ?? 0) + (x.Iskonto2 ?? 0) + (x.Iskonto3 ?? 0)
              + (x.Iskonto4 ?? 0) + (x.Iskonto5 ?? 0) + (x.Iskonto6 ?? 0);

            return (toplamIskontoTutar / brut) * 100.0;
        }

        // --------------------------------------------------------------------
        //  LISTE (EVRAK BAZLI TEK SATIR)
        // --------------------------------------------------------------------
        public async Task<IActionResult> Index(
            DateTime? startDate,
            DateTime? endDate,
            string? search,
            string? sort,
            int page = 1,
            int pageSize = 10)
        {
            var allowedPageSizes = new[] { 10, 18, 25, 50 };
            if (!allowedPageSizes.Contains(pageSize))
                pageSize = 10;

            var baseQuery = _db.Siparisler
                .AsNoTracking()
                .Where(s => s.SipTarih.HasValue &&
                            (s.Iptal == null || s.Iptal == false) &&
                            s.EvrakSira.HasValue &&
                            s.EvrakSeri != null);

            var lastDateInData = await baseQuery.MaxAsync(s => s.SipTarih);
            if (lastDateInData == null)
            {
                return View(new SiparisListViewModel
                {
                    Page = 1,
                    TotalPages = 0,
                    PageSize = pageSize,
                    TotalCount = 0
                });
            }

            var refDate = lastDateInData.Value.Date;

            if (!startDate.HasValue || !endDate.HasValue)
            {
                endDate ??= refDate;
                startDate ??= refDate.AddDays(-29);
            }

            var query = baseQuery.Where(s =>
                s.SipTarih >= startDate!.Value.Date &&
                s.SipTarih <= endDate!.Value.Date);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                // Not: liste evrak bazlı olacak ama arama satır üzerinden yapılabilir
                query = query.Where(s =>
                    (s.MusteriKod != null && s.MusteriKod.Contains(search)) ||
                    (s.SaticiKod != null && s.SaticiKod.Contains(search)) ||
                    (s.StokKod != null && s.StokKod.Contains(search)));
            }

            // ✅ Filtreli satırları çek (evrak bazlı gruplamak için)
            var rows = await query.ToListAsync();

            // ✅ Evrak bazlı özet üret
            var ozetler = rows
                .GroupBy(x => new { x.EvrakSeri, EvrakSira = x.EvrakSira!.Value })
                .Select(g =>
                {
                    var first = g.First();
                    return new SiparisOzetDto
                    {
                        EvrakSeri = first.EvrakSeri,
                        EvrakSira = first.EvrakSira!.Value,
                        Tarih = first.SipTarih,
                        MusteriKod = first.MusteriKod,
                        SaticiKod = first.SaticiKod,
                        ToplamMiktar = g.Sum(r => r.Miktar ?? 0),
                        ToplamTutarTl = g.Sum(HesaplaNetTutarTL) // ✅ satırların kendi kuruyla TL’ye çevirip topla
                    };
                })
                .ToList();

            // ✅ Üst kartlar
            var totalCount = ozetler.Count;
            var totalAmountTL = ozetler.Sum(x => x.ToplamTutarTl);
            var totalAmountKdvli = totalAmountTL * 1.20;
            var totalQty = ozetler.Sum(x => x.ToplamMiktar);

            // ✅ Sıralama
            IEnumerable<SiparisOzetDto> ordered =
                (sort == "tarih_asc")
                    ? ozetler.OrderBy(x => x.Tarih).ThenBy(x => x.EvrakSeri).ThenBy(x => x.EvrakSira)
                    : ozetler.OrderByDescending(x => x.Tarih).ThenByDescending(x => x.EvrakSira);

            // ✅ Sayfalama
            if (page < 1) page = 1;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var pageItems = ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ✅ Sayfadaki müşteri/satıcı adları
            var musteriKodlar = pageItems
                .Where(x => !string.IsNullOrEmpty(x.MusteriKod))
                .Select(x => x.MusteriKod!)
                .Distinct()
                .ToList();

            var cariList = await _db.CariHesaplar
                .AsNoTracking()
                .Where(c => musteriKodlar.Contains(c.CariKod))
                .ToListAsync();

            var musteriAdlari = cariList
                .ToDictionary(c => c.CariKod, c => c.Unvan1 ?? c.CariKod);

            var saticiKodlar = pageItems
                .Where(x => !string.IsNullOrEmpty(x.SaticiKod))
                .Select(x => x.SaticiKod!)
                .Distinct()
                .ToList();

            var saticiList = await _db.SorumluAdlari
                .AsNoTracking()
                .Where(x => saticiKodlar.Contains(x.SaticiKod))
                .ToListAsync();

            var saticiAdlari = saticiList
                .ToDictionary(
                    x => x.SaticiKod,
                    x => string.IsNullOrWhiteSpace(x.SaticiAd) ? x.SaticiKod : x.SaticiAd
                );

            var model = new SiparisListViewModel
            {
                Items = pageItems,
                StartDate = startDate,
                EndDate = endDate,
                Search = search,

                Page = page,
                TotalPages = totalPages,
                PageSize = pageSize,

                TotalCount = totalCount,
                TotalAmount = totalAmountTL,
                TotalAmountKdvli = totalAmountKdvli,
                TotalQuantity = totalQty,

                MusteriAdlari = musteriAdlari,
                SaticiAdlari = saticiAdlari
            };

            return View(model);
        }

        // --------------------------------------------------------------------
        //  DETAY - PARTIAL
        // --------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> DetayPartial(string seri, int sira)
        {
            if (string.IsNullOrWhiteSpace(seri))
                return BadRequest("Evrak seri boş.");

            var satirlar = await _db.Siparisler
                .AsNoTracking()
                .Where(s =>
                    (s.Iptal == null || s.Iptal == false) &&
                    s.EvrakSeri == seri &&
                    s.EvrakSira == sira)
                .OrderBy(s => s.SatirNo)
                .ToListAsync();

            if (!satirlar.Any())
                return NotFound("Bu evrak için sipariş satırı bulunamadı.");

            var first = satirlar.First();

            // Müşteri adı
            string? musteriAd = null;
            if (!string.IsNullOrEmpty(first.MusteriKod))
            {
                var cari = await _db.CariHesaplar
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CariKod == first.MusteriKod);

                musteriAd = cari?.Unvan1;
            }

            // Satıcı adı
            string? saticiAd = null;
            if (!string.IsNullOrEmpty(first.SaticiKod))
            {
                var satici = await _db.SorumluAdlari
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SaticiKod == first.SaticiKod);

                saticiAd = string.IsNullOrWhiteSpace(satici?.SaticiAd)
                    ? first.SaticiKod
                    : satici.SaticiAd;
            }

            // Stok adları sözlüğü
            var stokKodlar = satirlar
                .Where(x => !string.IsNullOrEmpty(x.StokKod))
                .Select(x => x.StokKod!)
                .Distinct()
                .ToList();

            var stokAdSozluk = new Dictionary<string, string>();
            if (stokKodlar.Any())
            {
                var stokList = await _db.StokAdlari
                    .AsNoTracking()
                    .Where(s => stokKodlar.Contains(s.StokKod))
                    .ToListAsync();

                stokAdSozluk = stokList
                    .ToDictionary(s => s.StokKod, s => s.StokAd);
            }

            var dtos = new List<SiparisDetaySatirDto>();

            foreach (var x in satirlar)
            {
                var miktar = x.Miktar ?? 0;
                var birim = x.BirimFiyat ?? 0;

                var brut = birim * miktar;
                var netDoviz = HesaplaNetTutarDoviz(x);

                var netBirim = (miktar != 0) ? (netDoviz / miktar) : 0;

                var dovizCinsi = x.DovizCinsi ?? (byte)0;
                var doviz = DovizSozluk.TryGetValue(dovizCinsi, out var sembol) ? sembol : null;

                var kur = (dovizCinsi == 0) ? 1.0 : (x.DovizKuru ?? 1.0);

                dtos.Add(new SiparisDetaySatirDto
                {
                    SatirNo = x.SatirNo,
                    StokKod = x.StokKod,
                    UrunAdi = (!string.IsNullOrEmpty(x.StokKod) && stokAdSozluk.TryGetValue(x.StokKod!, out var ad))
                        ? ad
                        : x.StokKod,
                    Aciklama = x.Aciklama,

                    Miktar = miktar,

                    BirimFiyat = birim,
                    IskontoYuzde = (brut == 0) ? 0 : ((brut - netDoviz) / brut) * 100.0,
                    NetBirimFiyat = netBirim,

                    Doviz = doviz,
                    Kur = kur,

                    TutarTL = netDoviz * kur
                });
            }

            var genelToplamTL = dtos.Sum(x => x.TutarTL ?? 0);
            var genelToplamKdvliTL = genelToplamTL * 1.20;

            var model = new SiparisDetayViewModel
            {
                EvrakSeri = first.EvrakSeri ?? seri,
                EvrakSira = first.EvrakSira ?? sira,
                Tarih = first.SipTarih,

                MusteriKod = first.MusteriKod,
                MusteriAd = musteriAd,

                SaticiKod = first.SaticiKod,
                SaticiAd = saticiAd,

                ToplamMiktar = dtos.Sum(x => x.Miktar ?? 0),
                GenelToplamTL = genelToplamTL,
                GenelToplamKdvliTL = genelToplamKdvliTL,

                Satirlar = dtos
            };

            return PartialView("_DetayPartial", model);
        }
    }
}