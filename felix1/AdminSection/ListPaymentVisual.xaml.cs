using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;

namespace felix1.AdminSection
{
    public partial class ListPaymentVisual : ContentView
    {
        private bool _unpaidOnly = false;
        public ObservableCollection<CombinedTransaction> CombinedItems { get; } = new();
        private readonly int _cashRegisterId;
        private bool _showOnlyUnpaid = false;

        public ListPaymentVisual(CashRegister cashRegister)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            InitializeComponent();
            _cashRegisterId = cashRegister.Id;
            LoadCombinedData();
            BindingContext = this;
        }

        private async void LoadCombinedData()
        {
            await ApplyFilters();
        }

        private async Task ApplyFilters()
        {
            await AppDbContext.ExecuteSafeAsync(async db =>
            {
                var orders = await db.Orders
                    .Include(o => o.CashRegister)
                    .Include(o => o.Items)
                    .Where(o => o.CashRegister != null && o.CashRegister.Id == _cashRegisterId)
                    .OrderByDescending(o => o.Date)
                    .ToListAsync();

                var orderIds = orders.Select(o => o.Id).ToList();
                var transactions = await db.Transactions
                    .Where(t => t.Order != null && orderIds.Contains(t.Order.Id))
                    .ToListAsync();

                var combinedItems = new List<CombinedTransaction>();

                foreach (var order in orders)
                {
                    if (_unpaidOnly && order.IsDuePaid)
                        continue;

                    decimal orderTotal = order.Items?.Sum(i => i.TotalPrice) ?? 0;
                    var orderTransactions = transactions.Where(t => t.Order?.Id == order.Id).ToList();

                    var item = new CombinedTransaction
                    {
                        Id = order.Id,
                        Time = (order.Date ?? DateTime.Now).ToString("HH:mm"),
                        ReferenceType = "Orden",
                        TotalAmount = orderTotal,
                        IsPaid = order.IsDuePaid,
                        PaymentMethods = GetPaymentMethods(orderTransactions),
                        OrderId = order.Id,
                        HasPayment = orderTransactions.Any()
                    };

                    combinedItems.Add(item);
                }

                var refunds = await db.Refunds
                    .Include(r => r.Order)
                        .ThenInclude(o => o!.CashRegister)
                    .Include(r => r.RefundedItems)
                    .Where(r => r.Order != null &&
                               r.Order.CashRegister != null &&
                               r.Order.CashRegister.Id == _cashRegisterId)
                    .ToListAsync();

                foreach (var refund in refunds)
                {
                    if (_unpaidOnly)
                        continue;

                    decimal refundTotal = refund.RefundedItems?.Sum(i => i.TotalPrice) ?? 0;

                    var item = new CombinedTransaction
                    {
                        Id = refund.Id,
                        Time = refund.Date.ToString("HH:mm"),
                        ReferenceType = "Reembolso",
                        TotalAmount = refundTotal,
                        IsPaid = true,
                        PaymentMethods = "Reembolso",
                        OrderId = refund.Order?.Id ?? 0,
                        IsRefund = true,
                        HasPayment = true
                    };

                    combinedItems.Add(item);
                }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    CombinedItems.Clear();
                    foreach (var item in combinedItems.OrderByDescending(i => i.Time))
                    {
                        CombinedItems.Add(item);
                    }
                });
            });
        }

        private async void OnFilterButtonClicked(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                _showOnlyUnpaid = !_showOnlyUnpaid;
                button.Text = _showOnlyUnpaid ? "Mostrar Todos" : "Filtrar";
                await ApplyFilters();
            }
        }

        private string GetPaymentMethods(List<Transaction> transactions)
        {
            if (transactions == null || !transactions.Any())
                return "Sin pago";

            var methods = new List<string>();
            var t = transactions.First();

            if (t.CashAmount > 0) methods.Add("Efectivo");
            if (t.CardAmount > 0) methods.Add("Tarjeta");
            if (t.TransferAmount > 0) methods.Add("Transferencia");

            return methods.Any() ? string.Join(", ", methods) : "Sin pago";
        }

        private string GetPaymentMethods(Transaction transaction)
        {
            var methods = new List<string>();
            if (transaction.CashAmount != 0) methods.Add("Efectivo");
            if (transaction.CardAmount > 0) methods.Add("Tarjeta");
            if (transaction.TransferAmount > 0) methods.Add("Transferencia");
            return methods.Count > 0 ? string.Join(", ", methods) : "Ninguno";
        }

        private async void OnDetailsClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton imageButton && imageButton.BindingContext is CombinedTransaction item)
            {
                if (Application.Current?.MainPage is NavigationPage navPage &&
                    navPage.CurrentPage is AdminSectionMainVisual adminPage)
                {
                    if (item.IsRefund)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Detalles",
                            "Esta transaccion esta asociada a un reembolso.",
                            "OK");
                    }
                    else
                    {
                        await AppDbContext.ExecuteSafeAsync(async db =>
                        {
                            var order = await db.Orders
                                .Include(o => o.CashRegister)
                                .FirstOrDefaultAsync(o => o.Id == item.OrderId);

                            if (order != null)
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    adminPage.SetRightPanelContent(new OrderDetailsVisual(item.OrderId));
                                });
                            }
                        });
                    }
                }
            }
        }

        private async void OnPayClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton imageButton && imageButton.BindingContext is CombinedTransaction item)
            {
                bool confirm = await Application.Current!.MainPage!.DisplayAlert(
                    "Confirmar pago",
                    $"�Marcar orden #{item.OrderId} como pagada?",
                    "S�", "No");

                if (confirm)
                {
                    await AppDbContext.ExecuteSafeAsync(async db =>
                    {
                        var order = await db.Orders.FindAsync(item.OrderId);
                        if (order != null)
                        {
                            order.IsDuePaid = true;
                            await db.SaveChangesAsync();
                            LoadCombinedData();
                        }
                    });
                }
            }
        }

        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                LoadCombinedData();
                return;
            }

            var filtered = CombinedItems.Where(p =>
                p.Id.ToString().Contains(searchText) ||
                p.Time.Contains(searchText) ||
                p.ReferenceType.ToLower().Contains(searchText) ||
                p.TotalAmount.ToString(CultureInfo.InvariantCulture).Contains(searchText) ||
                (p.IsPaid ? "pagado" : "sin pagar").Contains(searchText) ||
                p.PaymentMethods.ToLower().Contains(searchText)
            ).ToList();

            CombinedItems.Clear();
            foreach (var item in filtered)
            {
                CombinedItems.Add(item);
            }
        }

        private void OnUnpaidOnlySwitchToggled(object sender, ToggledEventArgs e)
        {
            _unpaidOnly = e.Value;
            LoadCombinedData();
        }

        private async void OnTogglePaymentStatusClicked(object sender, EventArgs e)
        {
            if (sender is ImageButton button && button.BindingContext is CombinedTransaction item)
            {
                bool newStatus = !item.IsPaid;
                string action = newStatus ? "marcar como PAGADA" : "marcar como PENDIENTE";

                bool confirm = await Application.Current!.MainPage!.DisplayAlert(
                    "Confirmar cambio",
                    $"�Desea {action} la orden #{item.OrderId}?",
                    "S�", "No");

                if (confirm)
                {
                    await AppDbContext.ExecuteSafeAsync(async db =>
                    {
                        var order = await db.Orders.FindAsync(item.OrderId);
                        if (order != null)
                        {
                            order.IsDuePaid = newStatus;
                            await db.SaveChangesAsync();
                            LoadCombinedData();
                        }
                    });
                }
            }
        }

        private void OnBackButtonClicked(object sender, EventArgs e)
        {
            if (Application.Current?.MainPage is NavigationPage navPage &&
                navPage.CurrentPage is AdminSectionMainVisual adminPage)
            {
                adminPage.SetRightPanelContent(new ListCashRegisterVisual());
            }
        }

        public class CombinedTransaction
        {
            public int Id { get; set; }
            public string Time { get; set; }
            public string ReferenceType { get; set; }
            public decimal TotalAmount { get; set; }
            public bool IsPaid { get; set; }
            public string PaymentStatus => IsPaid ? "Pagado" : "Pendiente";
            public string PaymentMethods { get; set; }
            public int OrderId { get; set; }
            public bool IsRefund { get; set; }
            public bool HasPayment { get; set; }
            public decimal DisplayAmount => IsRefund ? TotalAmount * -1 : TotalAmount;
            public string DisplayId => IsRefund ? $"{Id} ({OrderId})" : Id.ToString("0000");
        }
    }

    public class BoolToPaidStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue ? (boolValue ? "Pagado" : "Sin Pagar") : "Sin Pagar";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool boolValue ? !boolValue : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TextToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() == parameter?.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isPressed && parameter is string colors)
            {
                var colorArray = colors.Split(':');
                return isPressed ? Color.FromArgb(colorArray[1]) : Color.FromArgb(colorArray[0]);
            }
            return Color.FromArgb("#005F8C");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}