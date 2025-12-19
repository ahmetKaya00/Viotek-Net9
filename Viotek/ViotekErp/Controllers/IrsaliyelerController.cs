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
    public class IrsaliyelerController : Controller
    {
        private readonly MikroDbContext _db;

        public IrsaliyelerController(MikroDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? startDate,
            DateTime? endDate,
            string? search,
            string? status,     // all | invoiced | waiting
            int page = 1,
            int pageSize = 25)
        {
            // güvenli sayfa boyutu
            var allowedPageSizes = new[] { 10, 18, 25, 50, 100 };
            if (!Array.Exists(allowedPageSizes, x => x == pageSize))
                pageSize = 25;

            // status default
            status = string.IsNullOrWhiteSpace(status) ? "all" : status;

            // verideki SON tarih (default aralık için)
            DateTime? lastDateInData = null;

            var connMax = _db.Database.GetDbConnection();
            if (connMax.State != ConnectionState.Open)
                await connMax.OpenAsync();

            try
            {
                using var cmdMax = connMax.CreateCommand();
                cmdMax.CommandText = @"SELECT MAX(sth_tarih) FROM STOK_HAREKETLERI WITH (NOLOCK)";
                var r = await cmdMax.ExecuteScalarAsync();
                if (r != null && r != DBNull.Value)
                    lastDateInData = Convert.ToDateTime(r);
            }
            finally
            {
                if (connMax.State == ConnectionState.Open)
                    await connMax.CloseAsync();
            }

            if (lastDateInData == null)
            {
                return View(new IrsaliyeListViewModel
                {
                    Page = 1,
                    TotalPages = 0,
                    PageSize = pageSize,
                    TotalCount = 0,
                    StartDate = startDate,
                    EndDate = endDate,
                    Search = search,
                    Status = status
                });
            }

            var refDate = lastDateInData.Value.Date;

            // kullanıcı tarih seçmediyse → son 30 gün
            if (!startDate.HasValue || !endDate.HasValue)
            {
                endDate ??= refDate;
                startDate ??= refDate.AddDays(-29);
            }

            // --- SQL (CTE) ---
            var sqlCte = @"
WITH X AS (
    SELECT
        sth_tarih                          AS Tarih,
        sth_evrakno_seri                   AS Seri,
        sth_evrakno_sira                   AS Sira,
        sth_cari_kodu                      AS CariKod,
        dbo.fn_CarininIsminiBul(0, sth_cari_kodu) AS CariUnvan,

        MIN(dbo.fn_StokIsmi(sth_stok_kod)) AS IlkUrunAdi,
        COUNT(sth_satirno)                 AS SatirSayisi,

        SUM(
            sth_tutar
            - (sth_iskonto1 + sth_iskonto2 + sth_iskonto3 + sth_iskonto4 + sth_iskonto5 + sth_iskonto6)
            + (sth_masraf1 + sth_masraf2 + sth_masraf3 + sth_masraf4 + sth_vergi + sth_ilave_edilecek_kdv + sth_masraf_vergi)
        ) AS ToplamTutar,

        CASE
            WHEN MAX(CASE WHEN (sth_fat_uid IS NULL OR sth_fat_uid = '00000000-0000-0000-0000-000000000000') THEN 0 ELSE 1 END) = 1
                THEN 1
            ELSE 0
        END AS Faturalandi
    FROM STOK_HAREKETLERI WITH (NOLOCK)
    WHERE
        sth_cari_cinsi = 0
        AND sth_tarih >= @p1 AND sth_tarih < @p2
        AND sth_cins IN (0,1,2,12)
        AND sth_normal_iade = 0
        AND sth_evrakno_seri IS NOT NULL
        AND sth_evrakno_sira IS NOT NULL
        AND sth_evraktip IN (13,1)
    GROUP BY
        sth_tarih, sth_evrakno_seri, sth_evrakno_sira, sth_cari_kodu
)
";

            // X üzerinde filtreler
            var sqlWhere = "WHERE 1=1\n";

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                sqlWhere += @"
AND (
    CariKod LIKE '%' + @search + '%'
 OR CariUnvan LIKE '%' + @search + '%'
 OR (Seri + ' ' + CAST(Sira AS nvarchar(20))) LIKE '%' + @search + '%'
 OR (Seri + CAST(Sira AS nvarchar(20))) LIKE '%' + @search + '%'
 OR IlkUrunAdi LIKE '%' + @search + '%'
)
";
            }

            sqlWhere += status switch
            {
                "invoiced" => " AND Faturalandi = 1\n",
                "waiting"  => " AND Faturalandi = 0\n",
                _          => ""
            };

            // COUNT
            var sqlCount = @"
;" + sqlCte + @"
SELECT COUNT(1)
FROM X
" + sqlWhere + @"
";

            // DATA
            var sqlPaged = @"
;" + sqlCte + @"
SELECT *
FROM X
" + sqlWhere + @"
ORDER BY Tarih DESC, Seri, Sira
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
";

            int totalCount = 0;
            var items = new List<IrsaliyeListRow>();

            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync();

            try
            {
                // COUNT
                using (var cmdCount = conn.CreateCommand())
                {
                    cmdCount.CommandText = sqlCount;

                    var p1 = cmdCount.CreateParameter(); p1.ParameterName = "@p1"; p1.Value = startDate.Value.Date; cmdCount.Parameters.Add(p1);
                    var p2 = cmdCount.CreateParameter(); p2.ParameterName = "@p2"; p2.Value = endDate.Value.Date.AddDays(1); cmdCount.Parameters.Add(p2);

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var ps = cmdCount.CreateParameter(); ps.ParameterName = "@search"; ps.Value = search; cmdCount.Parameters.Add(ps);
                    }

                    var countObj = await cmdCount.ExecuteScalarAsync();
                    totalCount = (countObj == null || countObj == DBNull.Value) ? 0 : Convert.ToInt32(countObj);
                }

                // sayfa hesabı
                if (page < 1) page = 1;
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                if (totalPages == 0) totalPages = 1;
                if (page > totalPages) page = totalPages;

                var skip = (page - 1) * pageSize;

                // DATA
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlPaged;

                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@p1"; p1.Value = startDate.Value.Date; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "@p2"; p2.Value = endDate.Value.Date.AddDays(1); cmd.Parameters.Add(p2);

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var ps = cmd.CreateParameter(); ps.ParameterName = "@search"; ps.Value = search; cmd.Parameters.Add(ps);
                    }

                    var pSkip = cmd.CreateParameter(); pSkip.ParameterName = "@skip"; pSkip.Value = skip; cmd.Parameters.Add(pSkip);
                    var pTake = cmd.CreateParameter(); pTake.ParameterName = "@take"; pTake.Value = pageSize; cmd.Parameters.Add(pTake);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var tarih = reader.IsDBNull(reader.GetOrdinal("Tarih")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("Tarih"));
                        var seri = reader.IsDBNull(reader.GetOrdinal("Seri")) ? "" : reader.GetString(reader.GetOrdinal("Seri"));
                        var sira = reader.IsDBNull(reader.GetOrdinal("Sira")) ? 0 : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("Sira")));
                        var cariKod = reader.IsDBNull(reader.GetOrdinal("CariKod")) ? "" : reader.GetString(reader.GetOrdinal("CariKod"));
                        var cariUnvan = reader.IsDBNull(reader.GetOrdinal("CariUnvan")) ? "" : reader.GetString(reader.GetOrdinal("CariUnvan"));
                        var urun = reader.IsDBNull(reader.GetOrdinal("IlkUrunAdi")) ? "" : reader.GetString(reader.GetOrdinal("IlkUrunAdi"));
                        var satirSayisi = reader.IsDBNull(reader.GetOrdinal("SatirSayisi")) ? 0 : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("SatirSayisi")));
                        var toplam = reader.IsDBNull(reader.GetOrdinal("ToplamTutar")) ? 0m : Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("ToplamTutar")));
                        var faturalandi = !reader.IsDBNull(reader.GetOrdinal("Faturalandi")) &&
                                          Convert.ToInt32(reader.GetValue(reader.GetOrdinal("Faturalandi"))) == 1;

                        items.Add(new IrsaliyeListRow
                        {
                            Tarih = tarih,
                            EvrakSeri = seri,
                            EvrakSira = sira,
                            CariKod = cariKod,
                            CariUnvan = cariUnvan,
                            IlkUrunAdi = urun,
                            SatirSayisi = satirSayisi,
                            ToplamTutar = toplam,
                            FaturaDurumu = faturalandi ? "Faturalandı" : "Fatura Bekliyor"
                        });
                    }
                }

                return View(new IrsaliyeListViewModel
                {
                    Items = items,
                    StartDate = startDate,
                    EndDate = endDate,
                    Search = search,
                    Status = status,

                    Page = page,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    PageSize = pageSize,
                    TotalCount = totalCount
                });
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                    await conn.CloseAsync();
            }
        }
    }
}