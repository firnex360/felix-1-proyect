using System;

namespace felix1.Logic;

public class Refund
{
    public int Id { get; set; } // Auto-generated
    public Order? Order { get; set; } = null; // Reference to Order
    public User? User { get; set; } = null; // Reference to User
    public DateTime Date { get; set; } = DateTime.Now; // Default to current date
    public List<OrderItem>? RefundedItems { get; set; } = null; // List of refunded items
    public string? Reason { get; set; } = null; // Reason for refund
}
