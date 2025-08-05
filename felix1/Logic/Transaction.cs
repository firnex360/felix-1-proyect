using System;

namespace felix1.Logic;

public class Transaction
{
    public int Id { get; set; } // Auto-generated
    public DateTime? Date { get; set; } = DateTime.Now; // Default date
    public decimal TotalAmount { get; set; } = 0; // Default total amount
    public decimal TaxAmountITBIS { get; set; } = 0; // Default tax amount for ITBIS
    public decimal TaxAmountWaiters { get; set; } = 0; // Default tax amount for waiters
    public decimal CashAmount { get; set; } = 0; // Default cash amount
    public decimal CardAmount { get; set; } = 0; // Default card amount
    public decimal TransferAmount { get; set; } = 0; // Default transfer amount
    public Order? Order { get; set; } = null; // Reference to Order
    public Refund? Refund { get; set; } = null; // Reference to Refund
}
