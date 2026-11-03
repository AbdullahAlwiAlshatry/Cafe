using CafeOrdering.Data;
using CafeOrdering.Models;
using CafeOrdering.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using QuestPDF.Drawing;

FontManager.RegisterFont(
    File.OpenRead("wwwroot/Fonts/Cairo-Regular.ttf"));

FontManager.RegisterFont(
    File.OpenRead("wwwroot/Fonts/Cairo-Bold.ttf"));

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IPasswordHasher<AdminUser>, PasswordHasher<AdminUser>>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHttpClient<ITelegramService, TelegramService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o => {
    o.LoginPath = "/Admin/Login"; o.AccessDeniedPath = "/Admin/Login"; o.Cookie.Name = "Cafe.Admin";
    o.SlidingExpiration = true; o.ExpireTimeSpan = TimeSpan.FromHours(8);
});
builder.Services.AddAuthorization();
QuestPDF.Settings.License = LicenseType.Community;
var app = builder.Build();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SeedData.InitializeAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminUser>>(), app.Configuration);
}
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseRouting(); app.UseAuthentication(); app.UseAuthorization();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
