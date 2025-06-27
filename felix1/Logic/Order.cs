using System;

namespace felix1.Logic;

public class Order
{
    public int Id { get; set; } // Auto-generated
    public int Number { get; set; } = 0;
    public DateTime? Date { get; set; } = null;
    public bool Charge { get; set; } = false;
    public User? Waiter { get; set; } = null;
    public Table? Table { get; set; } = null;
    public CashRegister? CashRegister { get; set; } = null;
    public Article Items { get; set; } = null; //temp

}
