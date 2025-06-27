using System;
using felix1.Logic;

namespace felix1.Logic
{
    public class CashRegister
    {
        public int Id { get; set; } // Auto-generated
        public int Number { get; set; } = 0;
        public float InitialMoney { get; set; } = 0;
        public DateTime TimeStarted { get; set; } = DateTime.Now;
        public DateTime? TimeFinish { get; set; } = null;
        public bool IsSecPrice { get; set; } = false;
        public bool IsOpen { get; set; } = false;
        public User? Cashier { get; set; } = null;
    }
}
