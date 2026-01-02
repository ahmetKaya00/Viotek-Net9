using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddScoped<ErpAuthService>();

// DB
builder.Services.AddDbContext<MikroDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MikroDb")));
builder.Services.AddDbContext<ServisDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ServisDb")));

// MVC
builder.Services.AddControllersWithViews()
#if DEBUG
    .AddRazorRuntimeCompilation()
#endif
    ;

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ sırası önemli
app.UseAuthentication();
app.UseAuthorization();

// ✅ admin seed (umutkartopu)
using (var scope = app.Services.CreateScope())
{
    var auth = scope.ServiceProvider.GetRequiredService<ErpAuthService>();
    await auth.EnsureAdminAsync();
}

// ✅ DEFAULT: ilk açılış Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();