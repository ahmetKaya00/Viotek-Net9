using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViotekErp.Models;
using ViotekErp.Services;

namespace ViotekErp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ErpAuthService _auth;

        public AdminController(ErpAuthService auth)
        {
            _auth = auth;
        }

        // /Admin/Users
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _auth.GetUsersAsync();
            return View(users);
        }

        // /Admin/CreateUser
        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            ViewBag.Saticilar = await _auth.GetSipSaticiKodlariAsync();
            return View();
        }

        // /Admin/CreateUser (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(
            string kullaniciAdi,
            string adSoyad,
            string sifre,
            string rol = "User",
            string? sipSaticiKod = null)
        {
            // hata durumunda dropdown boş kalmasın
            ViewBag.Saticilar = await _auth.GetSipSaticiKodlariAsync();

            if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
            {
                ViewBag.Error = "Kullanıcı adı ve şifre zorunludur.";
                return View();
            }

            var isAdmin = string.Equals(rol, "Admin", StringComparison.OrdinalIgnoreCase);

            // Admin değilse sip_satici_kod zorunlu
            if (!isAdmin && string.IsNullOrWhiteSpace(sipSaticiKod))
            {
                ViewBag.Error = "User rolü için Sipariş Satıcı Kodu (sip_satici_kod) seçmek zorunludur.";
                return View();
            }

            var user = new erp_kullanici
            {
                KullaniciAdi = kullaniciAdi.Trim(),
                AdSoyad = string.IsNullOrWhiteSpace(adSoyad) ? null : adSoyad.Trim(),
                Rol = isAdmin ? "Admin" : "User",
                SifreHash = sifre, // düz şifre (sen böyle istedin)
                AktifMi = true,
                SipSaticiKod = isAdmin ? null : sipSaticiKod?.Trim(),
                GuncellemeTarihi = DateTime.UtcNow
            };

            try
            {
                await _auth.AddUserAsync(user);
            }
            catch
            {
                ViewBag.Error = "Bu kullanıcı adı zaten var (veya kayıt sırasında hata oluştu).";
                return View();
            }

            return RedirectToAction(nameof(Users));
        }

        // /Admin/Toggle (POST) => aktif/pasif
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int id)
        {
            // admin kendini kapatmasın
            var me = User.Identity?.Name;
            var u = await _auth.GetByIdAsync(id);
            if (u != null && u.KullaniciAdi == me)
                return RedirectToAction(nameof(Users));

            await _auth.ToggleActiveAsync(id);
            return RedirectToAction(nameof(Users));
        }

        // /Admin/ChangePassword (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(int id, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return RedirectToAction(nameof(Users));

            await _auth.UpdatePasswordAsync(id, newPassword);
            return RedirectToAction(nameof(Users));
        }

        // /Admin/Delete (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // admin kendini silmesin
            var me = User.Identity?.Name;
            var u = await _auth.GetByIdAsync(id);
            if (u != null && u.KullaniciAdi == me)
                return RedirectToAction(nameof(Users));

            await _auth.DeleteUserAsync(id);
            return RedirectToAction(nameof(Users));
        }
    }
}