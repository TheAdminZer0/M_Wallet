using System.Collections.Generic;

namespace M_Wallet.Shared.Models
{
    public class UserPreferences
    {
        public bool IsDarkMode { get; set; }
        public Dictionary<string, Dictionary<string, bool>> TableColumns { get; set; } = new();
        public List<string> FavoriteRoutes { get; set; } = new();
        public bool EnableImageResizing { get; set; } = true;
        public int MaxImageResolution { get; set; } = 800;
    }
}
