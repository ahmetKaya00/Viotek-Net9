using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ViotekErp.Services;

namespace ViotekErp.Controllers
{
    public class AuthController : Controller
    {
        private readonly ErpAuthService _auth;

        public AuthController(ErpAuthService auth)
        {
            _auth = auth;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string kullaniciAdi, string sifre, string? returnUrl = null)
        {
            var user = await _auth.LoginAsync(kullaniciAdi, sifre);

            if (user == null)
            {
                ViewBag.Error = "Kullanıcı adı veya şifre hatalı.";
                ViewBag.ReturnUrl = returnUrl;
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.KullaniciAdi),
                new Claim(ClaimTypes.Role, user.Rol ?? "User"),
                new Claim("AdSoyad", user.AdSoyad ?? "")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Auth");
    }

        [HttpGet]
        public IActionResult Denied()
        {
            return View();
        }
    }
}