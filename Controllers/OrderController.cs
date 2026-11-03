using System.Security.Cryptography;
using System.Text.Json;
using CafeOrdering.Data;
using CafeOrdering.Models;
using CafeOrdering.Services;
using CafeOrdering.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CafeOrdering.Controllers;

public class OrderController(AppDbContext db, IEmailService email, ITelegramService telegram, IConfiguration config, ILogger<OrderController> log) : Controller
{
    [HttpGet] public IActionResult Review() => View();
    [HttpGet] public IActionResult Checkout() => View(new CheckoutViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm); List<CartLine>? cart; try { cart = JsonSerializer.Deserialize<List<CartLine>>(vm.CartJson, new JsonSerializerOptions(JsonSerializerDefaults.Web)); } catch { cart = null; }
        if (cart == null || cart.Count == 0) { ModelState.AddModelError("", "السلة فارغة أو غير صالحة."); return View(vm); }
        var ids = cart.Where(x => x.Quantity > 0 && x.Quantity <= 50).Select(x => x.ProductId).Distinct().ToList(); var products = await db.Products.Where(p => ids.Contains(p.Id) && p.IsAvailable).ToDictionaryAsync(p => p.Id); if (products.Count == 0) { ModelState.AddModelError("", "لا توجد منتجات صالحة في الطلب."); return View(vm); }
        var order = new Order { CustomerName = vm.CustomerName.Trim(), Phone = vm.Phone.Trim(), Email = vm.Email.Trim(), Location = vm.Location.Trim(), LocationDetails = vm.LocationDetails.Trim(), Status = "Pending", VerificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), TokenExpiresAt = DateTime.UtcNow.AddMinutes(30) };
        foreach (var line in cart.Where(x => x.Quantity > 0 && x.Quantity <= 50)) { if (products.TryGetValue(line.ProductId, out var p)) { order.Items.Add(new OrderItem { ProductId = p.Id, ProductName = p.Name, UnitPrice = p.Price, Quantity = line.Quantity }); } }
        if (order.Items.Count == 0) { ModelState.AddModelError("", "تعذر قراءة عناصر الطلب."); return View(vm); }
        order.Total = order.Items.Sum(x => x.UnitPrice * x.Quantity); db.Orders.Add(order); await db.SaveChangesAsync(); var baseUrl = config["Email:BaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}"; var link = $"{baseUrl}/Order/Verify?token={Uri.EscapeDataString(order.VerificationToken)}"; if (string.IsNullOrWhiteSpace(config["Email:User"]) || string.IsNullOrWhiteSpace(config["Email:AppPassword"])) TempData["DevVerificationLink"] = link; try { await email.SendVerificationAsync(order.Email, order.CustomerName, link); } catch (Exception ex) { log.LogError(ex, "Email sending failed for order {Id}", order.Id); TempData["Notice"] = "تم حفظ طلبك، لكن تعذر إرسال رسالة التحقق. يرجى التواصل مع خدمة العملاء."; return RedirectToAction(nameof(Pending), new { id = order.Id }); }
        return RedirectToAction(nameof(Pending), new { id = order.Id });
    }
    [HttpGet] public async Task<IActionResult> Pending(int id) { var o = await db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id); if (o == null) return NotFound(); ViewBag.Notice = TempData["Notice"]; ViewBag.DevVerificationLink = TempData["DevVerificationLink"]; return View(o); }
    [HttpGet] public async Task<IActionResult> Verify(string token) { if (string.IsNullOrWhiteSpace(token)) return BadRequest(); var o = await db.Orders.Include(x => x.Items).FirstOrDefaultAsync(x => x.VerificationToken == token); if (o == null) return View("VerifyResult", false); if (o.Status == "Confirmed") return View("VerifyResult", true); if (o.TokenExpiresAt < DateTime.UtcNow) return View("VerifyExpired"); o.Status = "Confirmed"; o.ConfirmedAt = DateTime.UtcNow; o.VerificationToken = ""; await db.SaveChangesAsync(); try { await telegram.SendOrderAsync(o); o.TelegramSent = true; await db.SaveChangesAsync(); } catch (Exception ex) { log.LogError(ex, "Telegram notification failed for order {Id}", o.Id); } return RedirectToAction(nameof(Success), new { id = o.Id }); }
    [HttpGet] public async Task<IActionResult> Success(int id) { var o = await db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Status == "Confirmed"); return o == null ? NotFound() : View(o); }
}
