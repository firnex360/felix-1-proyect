using System;
using System.ComponentModel;

namespace felix1.Logic;

public class OrderItem : INotifyPropertyChanged
{
    public int Id { get; set; } // Auto-generated in database
    public Article? Article { get; set; } = null;
    
    private int _quantity = 0;
    public int Quantity 
    { 
        get => _quantity;
        set 
        {
            _quantity = value;
            OnPropertyChanged(nameof(TotalPrice));
        }
    }
    
    private decimal _unitPrice = 0;
    public decimal UnitPrice 
    { 
        get => _unitPrice;
        set 
        {
            _unitPrice = value;
            OnPropertyChanged(nameof(TotalPrice));
        }
    }
    
    // Properties for DataGrid binding
    public string ArticleName => Article?.Name ?? "";
    public decimal TotalPrice => Quantity * UnitPrice;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
