using System;
using System.Collections.Generic;

namespace M_Wallet.Shared
{
    public class Purchase
    {
        public int Id { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public string? SupplierName { get; set; }
        public decimal TotalAmount { get; set; }
        public List<PurchaseItem> Items { get; set; } = new();
        public string PaymentStatus { get; set; } = "Paid"; // Paid, Pending (Credit)
        public string PaidBy { get; set; } = "Store"; // Store, Employee Name
    }
}
