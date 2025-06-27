using System;

namespace felix1.Logic;

public class Table
{
    public int Id { get; set; } // Auto-generated
    public int LocalNumber { get; set; } = 0;
    public int GlobalNumber { get; set; } = 0;
    public bool IsTakeOut { get; set; } = false;
    public bool IsBillRequested { get; set; } = false;
    public bool IsPaid { get; set; } = false;
}
