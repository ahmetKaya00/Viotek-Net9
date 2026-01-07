using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using ViotekErp.Data;
using ViotekErp.Services;

var builder = WebApplication.CreateBuilder(args);

// Auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Auth/Login";
        o.AccessDeniedPath = "/Auth/Denied";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

// Services (DI)
builder.Services.AddScoped<ErpAuthService>();
builder.Services.AddScoped<ServisPdfService>();

// DB
builder.Services.AddDbContext<MikroDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MikroDb")));

builder.Services.AddDbContext<ServisDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ServisDb")));

// MVC
builder.Services.AddControllersWithViews()
#if DEBUG
    .AddRazorRuntimeCompilation()
#endif
    ;

var app = builder.Build();

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

// Font (opsiyonel: dosya yoksa patlamasın)
var fontPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "fonts", "DejaVuSans.ttf");
if (System.IO.File.Exists(fontPath))
{
    FontManager.RegisterFont(System.IO.File.OpenRead(fontPath));
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// admin seed
using (var scope = app.Services.CreateScope())
{
    var auth = scope.ServiceProvider.GetRequiredService<ErpAuthService>();
    await auth.EnsureAdminAsync();
}

// routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();