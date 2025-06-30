using System;

namespace felix1.Logic;

public class OrderItem
{
    public int Id { get; set; } // Auto-generated in database
    public Article? Article { get; set; } = null;
    public int Quantity { get; set; } = 0; 
    public decimal UnitPrice { get; set; } = 0; 
}
