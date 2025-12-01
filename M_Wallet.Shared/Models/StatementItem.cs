using System;

namespace M_Wallet.Shared
{
    public class StatementItem
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal RunningBalance { get; set; }
        public string Type { get; set; } = string.Empty;
    }
}
