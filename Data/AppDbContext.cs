using CafeOrdering.Models;
using Microsoft.EntityFrameworkCore;
namespace CafeOrdering.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
 public DbSet<Product> Products => Set<Product>(); public DbSet<ProductImage> ProductImages => Set<ProductImage>(); public DbSet<Order> Orders => Set<Order>(); public DbSet<OrderItem> OrderItems => Set<OrderItem>(); public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
 protected override void OnModelCreating(ModelBuilder b) { b.Entity<Product>().Property(x=>x.Price).HasPrecision(10,2); b.Entity<Order>().Property(x=>x.Total).HasPrecision(10,2); b.Entity<OrderItem>().Property(x=>x.UnitPrice).HasPrecision(10,2); b.Entity<Product>().HasMany(x=>x.Images).WithOne(x=>x.Product).HasForeignKey(x=>x.ProductId).OnDelete(DeleteBehavior.Cascade); b.Entity<Order>().HasMany(x=>x.Items).WithOne(x=>x.Order).HasForeignKey(x=>x.OrderId).OnDelete(DeleteBehavior.Cascade); b.Entity<AdminUser>().HasIndex(x=>x.Username).IsUnique(); }
}
