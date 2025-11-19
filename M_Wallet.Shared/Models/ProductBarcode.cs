namespace M_Wallet.Shared;

public class ProductBarcode
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public Product? Product { get; set; }
}
