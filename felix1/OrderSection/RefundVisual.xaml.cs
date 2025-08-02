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

    private List<OrderItem> _selectedOriginalItems = new();
    private List<OrderItem> _selectedRefundedItems = new();
    private bool _canExecuteCommands = true;

    public RefundVisual(Order order)
    {
        InitializeComponent();

        // Initialize commands first
        DiscardCommand = new Command(async () => await DiscardAsync(), () => _canExecuteCommands);
        ProcessRefundCommand = new Command(async () => await ProcessRefundAsync(), () => _canExecuteCommands);

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

        // Set BindingContext after initializing everything
        BindingContext = this;
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
        _selectedOriginalItems = e.AddedRows.Cast<OrderItem>().ToList();
    }

    private void OnRefundedItemSelected(object sender, DataGridSelectionChangedEventArgs e)
    {
        _selectedRefundedItems = e.AddedRows.Cast<OrderItem>().ToList();
    }

    private void OnAddSelectedToRefund(object sender, EventArgs e)
    {
        if (_selectedOriginalItems == null || _selectedOriginalItems.Count == 0)
        {
            DisplayAlert("Advertencia", "Por favor seleccione al menos un artículo para devolver.", "OK");
            return;
        }

        foreach (var selectedItem in _selectedOriginalItems.ToList())
        {
            if (selectedItem.Quantity > 0)
            {
                var existingItem = RefundedItems.FirstOrDefault(i => i.Article?.Id == selectedItem.Article?.Id);

                if (existingItem != null)
                {
                    existingItem.Quantity += 1;
                }
                else
                {
                    RefundedItems.Add(new OrderItem
                    {
                        Article = selectedItem.Article,
                        Quantity = 1,
                        UnitPrice = selectedItem.UnitPrice
                    });
                }

                selectedItem.Quantity -= 1;

                if (selectedItem.Quantity <= 0)
                {
                    OriginalItems.Remove(selectedItem);
                }
            }
        }

        OnPropertyChanged(nameof(OrderTotal));
        OnPropertyChanged(nameof(RefundTotal));
    }

    private void OnAddAllToRefund(object sender, EventArgs e)
    {
        if (OriginalItems.Count == 0)
        {
            DisplayAlert("Advertencia", "No hay artículos para devolver.", "OK");
            return;
        }

        foreach (var item in OriginalItems.ToList())
        {
            var existingItem = RefundedItems.FirstOrDefault(i => i.Article?.Id == item.Article?.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                RefundedItems.Add(new OrderItem
                {
                    Article = item.Article,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            OriginalItems.Remove(item);
        }

        OnPropertyChanged(nameof(OrderTotal));
        OnPropertyChanged(nameof(RefundTotal));
    }

    private void OnRemoveSelectedFromRefund(object sender, EventArgs e)
    {
        if (_selectedRefundedItems == null || _selectedRefundedItems.Count == 0)
        {
            DisplayAlert("Advertencia", "Por favor seleccione al menos un artículo para quitar de la devolución.", "OK");
            return;
        }

        foreach (var selectedItem in _selectedRefundedItems.ToList())
        {
            var existingItem = OriginalItems.FirstOrDefault(i => i.Article?.Id == selectedItem.Article?.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += selectedItem.Quantity;
            }
            else
            {
                OriginalItems.Add(new OrderItem
                {
                    Article = selectedItem.Article,
                    Quantity = selectedItem.Quantity,
                    UnitPrice = selectedItem.UnitPrice
                });
            }

            RefundedItems.Remove(selectedItem);
        }

        OnPropertyChanged(nameof(OrderTotal));
        OnPropertyChanged(nameof(RefundTotal));
    }

    private void OnRemoveAllFromRefund(object sender, EventArgs e)
    {
        if (RefundedItems.Count == 0)
        {
            DisplayAlert("Advertencia", "No hay artículos en la lista de devolución.", "OK");
            return;
        }

        foreach (var item in RefundedItems.ToList())
        {
            var existingItem = OriginalItems.FirstOrDefault(i => i.Article?.Id == item.Article?.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
            }
            else
            {
                OriginalItems.Add(new OrderItem
                {
                    Article = item.Article,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            RefundedItems.Remove(item);
        }

        OnPropertyChanged(nameof(OrderTotal));
        OnPropertyChanged(nameof(RefundTotal));
    }

    private async Task DiscardAsync()
    {
        if (!_canExecuteCommands) return;

        _canExecuteCommands = false;
        ((Command)DiscardCommand).ChangeCanExecute();

        try
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
        finally
        {
            _canExecuteCommands = true;
            ((Command)DiscardCommand).ChangeCanExecute();
        }
    }

    private async Task ProcessRefundAsync()
    {
        if (!_canExecuteCommands) return;

        _canExecuteCommands = false;
        ((Command)ProcessRefundCommand).ChangeCanExecute();

        try
        {
            if (RefundedItems.Count == 0)
            {
                await DisplayAlert("Error", "Debe seleccionar al menos un artículo para devolver.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Refund.Reason))
            {
                await DisplayAlert("Error", "Debe ingresar un motivo para la devolución.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Confirmar", "¿Deseas procesar la devolución?", "Sí", "No");
            if (!confirm) return;

            // Assign items to refund
            Refund.RefundedItems = RefundedItems.ToList();

            bool success = await AppDbContext.ExecuteSafeAsync(async db =>
            {
                // Attach existing order and user
                db.Orders.Attach(Refund.Order!);
                db.Users.Attach(Refund.User!);

                foreach (var item in Refund.RefundedItems)
                {
                    db.Articles.Attach(item.Article!);
                }

                // Add the refund
                await db.Refunds.AddAsync(Refund);
                await db.SaveChangesAsync();

                // Create the transaction with negative amount
                var transaction = new Transaction
                {
                    Date = DateTime.Now,
                    CashAmount = -RefundTotal,  // Negative amount for refund
                    TotalAmount = -RefundTotal, // Negative amount for refund
                    Refund = Refund,
                    Order = null, // Explicitly set to null to ensure it's not related to original order
                    CardAmount = 0,
                    TransferAmount = 0,
                    TaxAmountITBIS = 0,
                    TaxAmountWaiters = 0
                };

                await db.Transactions.AddAsync(transaction);
                await db.SaveChangesAsync();

                return true;
            },
            async ex => await DisplayAlert("Error", "Ocurrió un error al guardar la devolución.", "OK"));

            if (success)
            {

                // Update the main order list after successful refund
                if (ListTableVisual.Instance != null)
                {
                    ListTableVisual.Instance.ReloadTM();
                }

                var window = this.GetParentWindow();
                if (window != null)
                {
                    Application.Current?.CloseWindow(window);
                }
            }
        }
        finally
        {
            _canExecuteCommands = true;
            ((Command)ProcessRefundCommand).ChangeCanExecute();
        }
    }
}