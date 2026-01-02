using Microsoft.EntityFrameworkCore;
using ViotekErp.Data;
using ViotekErp.Models;

namespace ViotekErp.Services
{
    public class ErpAuthService
    {
        private readonly MikroDbContext _db;

        public ErpAuthService(MikroDbContext db)
        {
            _db = db;
        }

        // Login (düz şifre)
        public async Task<erp_kullanici?> LoginAsync(string kullaniciAdi, string sifre)
        {
            kullaniciAdi = (kullaniciAdi ?? "").Trim();

            var user = await _db.erp_kullanici
                .FirstOrDefaultAsync(x =>
                    x.KullaniciAdi == kullaniciAdi &&
                    x.SifreHash == sifre &&
                    x.AktifMi);

            if (user != null)
            {
                user.SonGirisTarihi = DateTime.UtcNow;
                user.GuncellemeTarihi = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return user;
        }

        public async Task<List<erp_kullanici>> GetUsersAsync()
        {
            return await _db.erp_kullanici
                .OrderByDescending(x => x.Rol == "Admin")
                .ThenBy(x => x.KullaniciAdi)
                .ToListAsync();
        }

        public async Task<erp_kullanici?> GetByIdAsync(int id)
        {
            return await _db.erp_kullanici.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task AddUserAsync(erp_kullanici user)
        {
            user.KullaniciAdi = user.KullaniciAdi.Trim();

            _db.erp_kullanici.Add(user);
            await _db.SaveChangesAsync();
        }

        public async Task UpdatePasswordAsync(int id, string newPassword)
        {
            var user = await _db.erp_kullanici.FindAsync(id);
            if (user == null) return;

            user.SifreHash = newPassword; // düz
            user.GuncellemeTarihi = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        public async Task ToggleActiveAsync(int id)
        {
            var user = await _db.erp_kullanici.FindAsync(id);
            if (user == null) return;

            user.AktifMi = !user.AktifMi;
            user.GuncellemeTarihi = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _db.erp_kullanici.FindAsync(id);
            if (user == null) return;

            _db.erp_kullanici.Remove(user);
            await _db.SaveChangesAsync();
        }

        // ✅ Admin seed: umutkartopu / 7c%8vm))
        public async Task EnsureAdminAsync()
        {
            var exists = await _db.erp_kullanici.AnyAsync(x => x.KullaniciAdi == "umutkartopu");
            if (exists) return;

            _db.erp_kullanici.Add(new erp_kullanici
            {
                KullaniciAdi = "umutkartopu",
                AdSoyad = "Umut Kartopu",
                Rol = "Admin",
                SifreHash = "7c%8vm))",
                AktifMi = true,
                SifreDegistir = false,
                OlusturmaTarihi = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }
        public async Task<List<string>> GetSipSaticiKodlariAsync()
{
    // SIPARISLER tablosundan satıcı kodları
    var list = await _db.Database
        .SqlQuery<string>($@"
            SELECT DISTINCT LTRIM(RTRIM(sip_satici_kod)) AS Value
            FROM dbo.SIPARISLER WITH (NOLOCK)
            WHERE sip_satici_kod IS NOT NULL AND LTRIM(RTRIM(sip_satici_kod)) <> ''
            ORDER BY LTRIM(RTRIM(sip_satici_kod))
        ")
        .ToListAsync();

    return list;
}
    }
}