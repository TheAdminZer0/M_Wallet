using System;
using System.Collections.Generic;

namespace M_Wallet.Shared;

public class Payment
{
    public int Id { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public string? PaymentMethod { get; set; } // Cash, Card, Transfer
    public string? Reference { get; set; } // Notes
    public string? CustomerName { get; set; } // Snapshot of customer name at time of payment
    public string? EmployeeName { get; set; } // Who collected the payment
    
    public List<PaymentAllocation> Allocations { get; set; } = new();
}
