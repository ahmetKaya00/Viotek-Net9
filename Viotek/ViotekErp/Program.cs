using Microsoft.EntityFrameworkCore;
using ViotekErp.Data;

var builder = WebApplication.CreateBuilder(args);

// --------------------------
// 1) SERVICES
// --------------------------

// SQL Server (MikroDB_V16_VIOTEK) bağlantısı
builder.Services.AddDbContext<MikroDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MikroDb")));

// MVC (Controller + View)
builder.Services.AddControllersWithViews()

#if DEBUG
    // İstersen bu satır için
    // Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
    // paketini eklemen gerekiyor
    .AddRazorRuntimeCompilation()
#endif
    ;

// --------------------------
// 2) APP PIPELINE
// --------------------------
var app = builder.Build();

// Hata sayfası ayarları
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// wwwroot altındaki css/js/img dosyaları için
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Default route: Dashboard açılacak
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
