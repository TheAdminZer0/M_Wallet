using System;
using System.ComponentModel.DataAnnotations;

namespace M_Wallet.Shared
{
    public class Person
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Role { get; set; } = "Customer"; // "Customer", "Driver", "Employee", "Admin"
        
        public string? PhoneNumber { get; set; }
        
        // Auth fields (for Employees/Admins/Drivers)
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Passcode { get; set; }
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public string? Preferences { get; set; } // JSON string for user settings

        // Driver specific
        public int CompletedDeliveries { get; set; } = 0;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public decimal Balance { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public DateTime? LastTransactionDate { get; set; }
    }
}
