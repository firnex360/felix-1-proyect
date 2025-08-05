using System;
using System.Collections.Generic;

namespace felix1.Logic;

public class PaymentReceipt
{
    public OrderReceipt orderReceipt { get; set; } = new OrderReceipt();
    public Transaction transaction { get; set; } = new Transaction();

    // Receipt Metadata
    public DateTime PrintDate { get; set; } = DateTime.Now;
}
