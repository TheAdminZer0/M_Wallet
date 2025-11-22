namespace M_Wallet.Shared;

public class PaymentAllocation
{
    public int Id { get; set; }
    
    public int PaymentId { get; set; }
    public Payment? Payment { get; set; }
    
    public int TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
    
    public decimal Amount { get; set; }
}
