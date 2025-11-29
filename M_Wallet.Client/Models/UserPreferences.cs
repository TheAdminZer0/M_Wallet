namespace M_Wallet.Client.Models
{
    public class UserPreferences
    {
        public bool IsDarkMode { get; set; }
        public Dictionary<string, Dictionary<string, bool>> TableColumns { get; set; } = new();
    }
}
