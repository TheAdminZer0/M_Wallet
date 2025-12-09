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
    public bool IsPinned { get; set; } = false;
    public bool IsService { get; set; } = false;
    public bool IsStockless { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int SoldLast30Days { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int TotalSoldQuantity { get; set; }
}