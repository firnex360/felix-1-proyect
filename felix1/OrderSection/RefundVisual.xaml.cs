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

        DiscardCommand = new Command(async () => await DiscardAsync());
        ProcessRefundCommand = new Command(async () => await ProcessRefundAsync());
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

    private async Task DiscardAsync()
    {
        bool confirm = await DisplayAlert("Confirmar", "¿Deseas descartar esta devolución?", "Sí", "No");
        if (confirm)
        {
            var window = this.GetParentWindow();
            if (window != null)
            {
                Application.Current?.CloseWindow(window);
            }
        }
    }

    private async Task ProcessRefundAsync()
    {
        if (RefundedItems.Count == 0)
        {
            await DisplayAlert("Error", "Debe seleccionar al menos un artículo para devolver.", "OK");
            return;
        }

        bool confirm = await DisplayAlert("Confirmar", "¿Deseas procesar la devolución?", "Sí", "No");
        if (!confirm) return;

        // Assign refunded items to the refund object
        Refund.RefundedItems = RefundedItems.ToList();

        await AppDbContext.ExecuteSafeAsync(async db =>
        {
            // Attach the existing order and user from the context if necessary
            db.Orders.Attach(Refund.Order);
            db.Users.Attach(Refund.User);

            foreach (var item in Refund.RefundedItems)
            {
                db.Articles.Attach(item.Article); // Needed for FK
            }

            // Add the refund with its items
            await db.Refunds.AddAsync(Refund);
            await db.SaveChangesAsync(); // Save to generate Refund.Id

            // Create the transaction
            var transaction = new Transaction
            {
                Date = DateTime.Now,
                CashAmount = RefundTotal,
                TotalAmount = RefundTotal,
                Refund = Refund
            };

            await db.Transactions.AddAsync(transaction);
            await db.SaveChangesAsync(); // Save everything
        },
        async ex => await DisplayAlert("Error", "Ocurrió un error al guardar la devolución.", "OK"));

        await DisplayAlert("Éxito", "La devolución fue procesada exitosamente.", "OK");

        var window = this.GetParentWindow();
        if (window != null)
        {
            Application.Current?.CloseWindow(window);
        }
    }

}
