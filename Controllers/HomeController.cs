using CafeOrdering.Data;
using CafeOrdering.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CafeOrdering.Controllers;

public class HomeController(AppDbContext db, IPdfService pdf) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Products.Where(p => p.IsAvailable).Include(p => p.Images).OrderBy(p => p.Name).ToListAsync());
    public async Task<IActionResult> MenuPdf() { var products = await db.Products.Where(p => p.IsAvailable).OrderBy(p => p.Name).ToListAsync(); return File(pdf.CreateMenu(products), "application/pdf", "cafe-menu.pdf"); }
    public IActionResult Error() => View();
}
