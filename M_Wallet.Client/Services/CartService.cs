using M_Wallet.Shared;

namespace M_Wallet.Client.Services;

public class CartService
{
    public List<TransactionItem> CartItems { get; set; } = new();
    public Person? SelectedCustomer { get; set; }
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string OrderNote { get; set; } = "";
    public string PaymentMethod { get; set; } = "Cash";
    public decimal AmountPaid { get; set; }
    public bool ApplyDiscount { get; set; }
    public decimal DiscountAmount { get; set; }
    public bool PrintReceipt { get; set; } = false;
    public string PrintFormat { get; set; } = "A4";
    public bool IsDelivery { get; set; } = false;
    public Person? SelectedDriver { get; set; }
    public string DriverName { get; set; } = "";

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
    }
}
