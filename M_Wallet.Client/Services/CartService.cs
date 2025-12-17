using M_Wallet.Shared;

namespace M_Wallet.Client.Services;

/// <summary>
/// Singleton service for maintaining cart state across page navigations.
/// Preserves cart items, customer/driver selection, and checkout options
/// when navigating away from the POS page and returning.
/// </summary>
public class CartService
{
    // ===== Cart Items =====
    public List<TransactionItem> CartItems { get; set; } = new();

    // ===== Customer Selection =====
    public Person? SelectedCustomer { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";

    // ===== Order Details =====
    public string OrderNote { get; set; } = "";
    public string PaymentMethod { get; set; } = "Cash";
    public decimal AmountPaid { get; set; }
    public bool ApplyDiscount { get; set; }
    public decimal DiscountAmount { get; set; }

    // ===== Print Options =====
    public bool PrintReceipt { get; set; } = false;
    public string PrintFormat { get; set; } = "A4";

    // ===== Delivery Options =====
    public bool IsDelivery { get; set; } = false;
    public Person? SelectedDriver { get; set; }
    public string DriverName { get; set; } = "";
    public string DriverPhone { get; set; } = "";

    /// <summary>
    /// Resets all cart state to default values.
    /// Called after successful checkout or manual cart clear.
    /// </summary>
    public void Clear()
    {
        CartItems.Clear();
        SelectedCustomer = null;
        CustomerName = "";
        CustomerPhone = "";
        OrderNote = "";
        PaymentMethod = "Cash";
        AmountPaid = 0;
        ApplyDiscount = false;
        DiscountAmount = 0;
        PrintReceipt = false;
        PrintFormat = "A4";
        IsDelivery = false;
        SelectedDriver = null;
        DriverName = "";
        DriverPhone = "";
    }
}
