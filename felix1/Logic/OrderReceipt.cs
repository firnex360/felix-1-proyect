using System;
using System.Collections.Generic;

namespace felix1.Logic;

public class OrderReceipt
{
    // Company Information
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string CompanyPhone { get; set; } = string.Empty;
    public string CompanyRNC { get; set; } = string.Empty;

    // Order Information (actual Order object)
    public Order? Order { get; set; } = null;

    // Transaction Financial Information (from Transaction class)
    public decimal TotalAmount { get; set; }
    public decimal TaxAmountITBIS { get; set; }
    public decimal TaxAmountWaiters { get; set; }
    public decimal CashAmount { get; set; }
    public decimal CardAmount { get; set; }
    public decimal TransferAmount { get; set; }

    // Receipt Metadata
    public DateTime PrintDate { get; set; } = DateTime.Now;
}
