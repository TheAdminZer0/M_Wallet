using System;
using System.ComponentModel.DataAnnotations;

namespace M_Wallet.Shared
{
    public class Customer
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? PhoneNumber { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
