using System;

namespace felix1.Logic;

public class Transaction
{
    public int Id { get; set; } // Auto-generated
    public DateTime? Date { get; set; } = null; // Default date
    public decimal TotalAmount { get; set; } = 0; // Default total amount
    public decimal TaxAmount { get; set; } = 0; // Default tax amount
    public decimal CashAmount { get; set; } = 0; // Default cash amount
    public decimal CardAmount { get; set; } = 0; // Default card amount
    public decimal TransferAmount { get; set; } = 0; // Default transfer amount
    public Order? Order { get; set; } = null; // Reference to Order
    public Refund? Refund { get; set; } = null; // Reference to Refund
}
