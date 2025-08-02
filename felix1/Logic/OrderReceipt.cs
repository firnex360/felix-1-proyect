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
    public decimal TaxAmountDelivery { get; set; } // Delivery/takeout tax amount
    public decimal CashAmount { get; set; }
    public decimal CardAmount { get; set; }
    public decimal TransferAmount { get; set; }

    // Tax Percentages
    public decimal TaxRateITBIS { get; set; } // ITBIS tax rate (e.g., 0.18 for 18%)
    public decimal TaxRateWaiters { get; set; } // Waiter tax rate (e.g., 0.10 for 10%)
    public decimal TaxRateDelivery { get; set; } // Delivery tax rate (e.g., 0.10 for 10%)

    // Receipt Metadata
    public DateTime PrintDate { get; set; } = DateTime.Now;
}
