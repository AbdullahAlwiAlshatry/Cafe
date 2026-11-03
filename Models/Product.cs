namespace CafeOrdering.Models;
public class Product { public int Id { get; set; } public string Name { get; set; } = ""; public string Description { get; set; } = ""; public decimal Price { get; set; } public string MainImage { get; set; } = "/images/coffee.svg"; public bool IsAvailable { get; set; } = true; public List<ProductImage> Images { get; set; } = new(); }
public class ProductImage { public int Id { get; set; } public int ProductId { get; set; } public string ImagePath { get; set; } = ""; public Product? Product { get; set; } }
