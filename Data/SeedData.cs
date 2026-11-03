using CafeOrdering.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace CafeOrdering.Data;
public static class SeedData {
 public static async Task InitializeAsync(AppDbContext db, IPasswordHasher<AdminUser> hasher, IConfiguration config) {
  if (!await db.AdminUsers.AnyAsync()) { var a=new AdminUser { Username=config["Admin:Username"] ?? "admin" }; a.PasswordHash=hasher.HashPassword(a,config["Admin:Password"] ?? "ChangeThisPassword123!"); db.AdminUsers.Add(a); }
  if (!await db.Products.AnyAsync()) { db.Products.AddRange(new Product{Name="كابتشينو",Description="إسبريسو مع الحليب المبخر ورغوة ناعمة.",Price=3.50m},new Product{Name="لاتيه",Description="قهوة إسبريسو مع حليب كريمي.",Price=3.75m},new Product{Name="موكا",Description="مزيج الشوكولاتة والإسبريسو والحليب.",Price=4.00m},new Product{Name="قهوة أمريكية",Description="قهوة سوداء بطعم متوازن.",Price=2.50m}); }
  await db.SaveChangesAsync();
 }
}
