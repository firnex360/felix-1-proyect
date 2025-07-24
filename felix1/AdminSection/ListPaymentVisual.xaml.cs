using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;

namespace felix1.AdminSection
{
    public partial class ListPaymentVisual : ContentView
    {
        public ObservableCollection<CombinedOrderPayment> CombinedItems { get; } = new();
        private readonly int _cashRegisterId;

        public ListPaymentVisual(CashRegister cashRegister)
        {
            InitializeComponent();
            _cashRegisterId = cashRegister.Id;
            LoadCombinedData();
            BindingContext = this;
        }

        private async void LoadCombinedData()
        {
            await AppDbContext.ExecuteSafeAsync(async db =>
            {
                var orders = await db.Orders
                    .Where(o => o.CashRegister != null && o.CashRegister.Id == _cashRegisterId)
                    .ToListAsync();

                var transactions = await db.Transactions
                    .Include(t => t.Order)
                    .Where(t => t.Order != null && 
                               t.Order.CashRegister != null && 
                               t.Order.CashRegister.Id == _cashRegisterId)
                    .ToListAsync();

                var combinedItems = new List<CombinedOrderPayment>();

                foreach (var order in orders)
                {
                    var transaction = transactions.FirstOrDefault(t => t.Order?.Id == order.Id);
                    
                    var item = new CombinedOrderPayment
                    {
                        Id = order.Id,
                        Time = (order.Date ?? DateTime.Now).ToString("HH:mm"), // Jaja lol no quiero que sea tanta información
                        OrderNumber = order.OrderNumber,
                        TotalAmount = transaction?.TotalAmount ?? 0,
                        IsPaid = transaction != null || order.IsDuePaid,
                        PaymentMethods = transaction != null ? GetPaymentMethods(transaction) : "Ninguno",
                        OrderId = order.Id,
                        HasTransaction = transaction != null
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

        private string GetPaymentMethods(Transaction transaction)
        {
            var methods = new List<string>();
            if (transaction.CashAmount > 0) methods.Add("Efectivo");
            if (transaction.CardAmount > 0) methods.Add("Tarjeta");
            if (transaction.TransferAmount > 0) methods.Add("Transferencia");
            return methods.Count > 0 ? string.Join(", ", methods) : "Ninguno";
        }

        private async void OnDetailsClicked(object sender, EventArgs e)
        {
        }

        private async void OnPayClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.BindingContext is CombinedOrderPayment item)
            {
                bool confirm = await Application.Current.MainPage.DisplayAlert(
                    "Confirmar pago",
                    $"¿Marcar orden #{item.OrderNumber} como pagada?",
                    "Sí", "No");

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
                p.OrderNumber.ToString().Contains(searchText) ||
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

        private void OnBackButtonClicked(object sender, EventArgs e)
        {
            if (Application.Current?.MainPage is NavigationPage navPage &&
                navPage.CurrentPage is AdminSectionMainVisual adminPage)
            {
                adminPage.SetRightPanelContent(new ListCashRegisterVisual());
            }
        }

        public class CombinedOrderPayment
        {
            public int Id { get; set; }
            public string Time { get; set; }
            public int OrderNumber { get; set; }
            public decimal TotalAmount { get; set; }
            public bool IsPaid { get; set; }

            public string PaymentStatus => IsPaid ? "Pagado" : "Sin Pagar";

            public string PaymentMethods { get; set; }
            public int OrderId { get; set; }
            public bool HasTransaction { get; set; }
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
}