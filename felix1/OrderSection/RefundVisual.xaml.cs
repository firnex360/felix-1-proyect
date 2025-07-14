using System.Collections.ObjectModel;
using System.Windows.Input;
using felix1.Logic;
using felix1.Data;
using Microsoft.Maui.Controls;
using Syncfusion.Maui.DataGrid;

namespace felix1.OrderSection;

public partial class RefundVisual : ContentPage
{
    public ObservableCollection<OrderItem> OriginalItems { get; set; } = new();
    public ObservableCollection<OrderItem> RefundedItems { get; set; } = new();
    public Refund Refund { get; private set; }
    public Order Order { get; private set; }

    public decimal OrderTotal => OriginalItems.Sum(i => i.TotalPrice);
    public decimal RefundTotal => RefundedItems.Sum(i => i.TotalPrice);

    public ICommand DiscardCommand { get; }
    public ICommand ProcessRefundCommand { get; }

    private OrderItem _selectedOriginalItem;
    private OrderItem _selectedRefundedItem;

    public RefundVisual(Order order)
    {
        InitializeComponent();
        BindingContext = this;

        Order = order;
        Refund = new Refund
        {
            Order = order,
            User = AppSession.CurrentUser,
            Date = DateTime.Now,
            RefundedItems = new List<OrderItem>(),
            Reason = string.Empty
        };

        LoadOrderItems();

        DiscardCommand = new Command(OnDiscard);
        ProcessRefundCommand = new Command(OnProcessRefund);
    }

    private void LoadOrderItems()
    {
        OriginalItems.Clear();
        RefundedItems.Clear();

        if (Order.Items == null) return;

        foreach (var item in Order.Items)
        {
            var copy = new OrderItem
            {
                Article = item.Article,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };
            OriginalItems.Add(copy);
        }
    }

    private void OnOriginalItemSelected(object sender, DataGridSelectionChangedEventArgs e)
    {
        if (e.AddedRows.Count > 0 && e.AddedRows[0] is OrderItem selectedItem)
        {
            _selectedOriginalItem = selectedItem;
        }
    }

    private void OnRefundedItemSelected(object sender, DataGridSelectionChangedEventArgs e)
    {
        if (e.AddedRows.Count > 0 && e.AddedRows[0] is OrderItem selectedItem)
        {
            _selectedRefundedItem = selectedItem;
        }
    }

    private void OnAddToRefund(object sender, EventArgs e)
    {
        if (_selectedOriginalItem != null && _selectedOriginalItem.Quantity > 0)
        {
            _selectedOriginalItem.Quantity -= 1;

            var existingItem = RefundedItems.FirstOrDefault(i => i.Article?.Id == _selectedOriginalItem.Article?.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += 1;
            }
            else
            {
                RefundedItems.Add(new OrderItem
                {
                    Article = _selectedOriginalItem.Article,
                    Quantity = 1,
                    UnitPrice = _selectedOriginalItem.UnitPrice
                });
            }

            if (_selectedOriginalItem.Quantity <= 0)
            {
                OriginalItems.Remove(_selectedOriginalItem);
                _selectedOriginalItem = null;
            }

            OnPropertyChanged(nameof(OrderTotal));
            OnPropertyChanged(nameof(RefundTotal));
        }
    }

    private void OnRemoveFromRefund(object sender, EventArgs e)
    {
        if (_selectedRefundedItem != null)
        {
            var existingItem = OriginalItems.FirstOrDefault(i => i.Article?.Id == _selectedRefundedItem.Article?.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += _selectedRefundedItem.Quantity;
            }
            else
            {
                OriginalItems.Add(new OrderItem
                {
                    Article = _selectedRefundedItem.Article,
                    Quantity = _selectedRefundedItem.Quantity,
                    UnitPrice = _selectedRefundedItem.UnitPrice
                });
            }

            RefundedItems.Remove(_selectedRefundedItem);
            _selectedRefundedItem = null;
            OnPropertyChanged(nameof(OrderTotal));
            OnPropertyChanged(nameof(RefundTotal));
        }
    }

    private async void OnDiscard()
    {
        bool confirm = await DisplayAlert("Confirmar", "¿Deseas descartar esta devolución?", "Sí", "No");
        if (confirm)
        {
            await Navigation.PopAsync();
        }
    }

    private async void OnProcessRefund()
    {
        if (RefundedItems.Count == 0)
        {
            await DisplayAlert("Error", "Debe seleccionar al menos un artículo para devolver.", "OK");
            return;
        }

        Refund.RefundedItems = RefundedItems.ToList();

        try
        {
            //base de datos here!!!!!


        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"OMG no funciona lol {ex.Message}", "OK");
        }
    }
}
