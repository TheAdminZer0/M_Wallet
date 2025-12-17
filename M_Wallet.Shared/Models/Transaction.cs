namespace M_Wallet.Shared;

public class Transaction
{
    public int Id { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public int? PersonId { get; set; }
    public Person? Person { get; set; }
    public string? CustomerName { get; set; } // Optional customer tracking
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? CustomerPhone { get; set; } // For creating new customers during transaction

    public string? Note { get; set; } // Optional note for the order
    public bool IsDelivery { get; set; }
    public int? DriverId { get; set; }
    public Person? Driver { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Discount { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public List<TransactionItem> Items { get; set; } = new();
    public List<PaymentAllocation> PaymentAllocations { get; set; } = new();
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;

    /// <summary>
    /// Calculates the total amount paid towards this transaction.
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal TotalPaid => PaymentAllocations?.Sum(pa => pa.Amount) ?? 0;

    /// <summary>
    /// Calculates the remaining balance due (TotalAmount minus all payments).
    /// </summary>
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal BalanceDue => TotalAmount - TotalPaid;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? DriverName { get; set; } // For creating new drivers during transaction

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? DriverPhone { get; set; } // For creating new drivers during transaction
}

public enum TransactionStatus
{
    Completed,
    Pending,
    Canceled
}

public class TransactionItem
{
    public int Id { get; set; }
    public int TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Subtotal { get; set; }
}