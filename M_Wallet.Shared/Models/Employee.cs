using System.ComponentModel.DataAnnotations;

namespace M_Wallet.Shared
{
    public class Employee
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Passcode { get; set; } // The pattern string, e.g., "0124678"
        
        public string Role { get; set; } = "Staff"; // "Admin", "Staff", "System"

        public string? Username { get; set; }
        public string? Password { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
