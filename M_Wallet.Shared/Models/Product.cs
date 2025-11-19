namespace M_Wallet.Shared;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal CostPrice { get; set; } = 0;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public List<ProductBarcode> Barcodes { get; set; } = new();
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}