using System;
using System.Collections.Generic;

namespace felix1.Logic;

public class Order
{
    public int Id { get; set; } // Auto-generated
    public int OrderNumber { get; set; } = 0; // Default order number
    public DateTime? Date { get; set; } = DateTime.Now; // Default date
    public bool IsBillRequested { get; set; } = false; // Default bill request status
    public bool IsDuePaid { get; set; } = false; // Default due payment status
    public User? Waiter { get; set; } = null; // Reference to waiter
    public Table? Table { get; set; } = null; // Reference to table
    public CashRegister? CashRegister { get; set; } = null; // Reference to cash register
    public List<OrderItem>? Items { get; set; } = null; // List of order items

}
