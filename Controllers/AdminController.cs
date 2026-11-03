using System.Security.Claims;
using CafeOrdering.Data;
using CafeOrdering.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CafeOrdering.Controllers;
public class AdminController(AppDbContext db,IPasswordHasher<AdminUser> hasher,IWebHostEnvironment env):Controller {
 [HttpGet] public IActionResult Login()=>User.Identity?.IsAuthenticated==true?RedirectToAction(nameof(Index)):View();
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Login(string username,string password){var a=await db.AdminUsers.SingleOrDefaultAsync(x=>x.Username==username);if(a==null||hasher.VerifyHashedPassword(a,a.PasswordHash,password)!=PasswordVerificationResult.Success){ViewBag.Error="اسم المستخدم أو كلمة المرور غير صحيحة";return View();}var claims=new[]{new Claim(ClaimTypes.Name,a.Username),new Claim(ClaimTypes.Role,"Admin")};await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));return RedirectToAction(nameof(Index));}
 [Authorize(Roles="Admin")] public async Task<IActionResult> Index()=>View(await db.Products.OrderBy(x=>x.Name).ToListAsync());
 [Authorize(Roles="Admin")] [HttpGet] public IActionResult Create()=>View("ProductForm",new Product());
 [Authorize(Roles="Admin")] [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Create(Product p,IFormFile? mainImage,List<IFormFile>? gallery){if(!ModelState.IsValid)return View("ProductForm",p);p.MainImage=await SaveImage(mainImage)??"/images/coffee.svg";if(gallery!=null)foreach(var f in gallery.Take(8)){var path=await SaveImage(f);if(path!=null)p.Images.Add(new ProductImage{ImagePath=path});}db.Products.Add(p);await db.SaveChangesAsync();return RedirectToAction(nameof(Index));}
 [Authorize(Roles="Admin")] [HttpGet] public async Task<IActionResult> Edit(int id){var p=await db.Products.Include(x=>x.Images).FirstOrDefaultAsync(x=>x.Id==id);return p==null?NotFound():View("ProductForm",p);}
 [Authorize(Roles="Admin")] [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Edit(int id,Product input,IFormFile? mainImage,List<IFormFile>? gallery){var p=await db.Products.Include(x=>x.Images).FirstOrDefaultAsync(x=>x.Id==id);if(p==null)return NotFound();if(!ModelState.IsValid){input.Id=id;return View("ProductForm",input);}p.Name=input.Name;p.Description=input.Description;p.Price=input.Price;p.IsAvailable=input.IsAvailable;var main=await SaveImage(mainImage);if(main!=null)p.MainImage=main;if(gallery!=null)foreach(var f in gallery.Take(8)){var path=await SaveImage(f);if(path!=null)p.Images.Add(new ProductImage{ImagePath=path});}await db.SaveChangesAsync();return RedirectToAction(nameof(Index));}
 [Authorize(Roles="Admin")] [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Delete(int id){var p=await db.Products.FindAsync(id);if(p!=null){db.Products.Remove(p);await db.SaveChangesAsync();}return RedirectToAction(nameof(Index));}
 [Authorize(Roles="Admin")] public async Task<IActionResult> Orders()=>View(await db.Orders.Include(x=>x.Items).OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync());
 [Authorize(Roles="Admin")] [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Logout(){await HttpContext.SignOutAsync();return RedirectToAction(nameof(Login));}
 private async Task<string?> SaveImage(IFormFile? file){if(file==null||file.Length==0)return null;var ext=Path.GetExtension(file.FileName).ToLowerInvariant();if(!new[]{".jpg",".jpeg",".png",".webp"}.Contains(ext)||file.Length>5*1024*1024)return null;var name=$"{Guid.NewGuid():N}{ext}";var dir=Path.Combine(env.WebRootPath,"uploads");Directory.CreateDirectory(dir);await using var stream=System.IO.File.Create(Path.Combine(dir,name));await file.CopyToAsync(stream);return "/uploads/"+name;}
}
