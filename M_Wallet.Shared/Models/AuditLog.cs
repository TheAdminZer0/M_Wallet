using System;

namespace M_Wallet.Shared;

public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, Sale
    public string Entity { get; set; } = string.Empty; // Product, Transaction, Purchase
    public string? EntityId { get; set; } // ID of the entity
    public string? EmployeeName { get; set; } // Who did it
    public string? Description { get; set; } // Details
    public string? Changes { get; set; } // JSON or text description of changes
}
