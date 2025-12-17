namespace M_Wallet.Client.Models
{
    public class ColumnDefinition
    {
        public string Title { get; set; } = string.Empty;
        public bool IsVisible { get; set; } = true;
    }

    /// <summary>
    /// Extension methods for column visibility checks.
    /// </summary>
    public static class ColumnDefinitionExtensions
    {
        /// <summary>
        /// Checks if a column with the given title is visible.
        /// Returns true if the column is not found (default visible).
        /// </summary>
        public static bool IsVisible(this List<ColumnDefinition> columns, string title)
        {
            return columns.FirstOrDefault(c => c.Title == title)?.IsVisible ?? true;
        }
    }
}
