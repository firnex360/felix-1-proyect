
using System.Collections.ObjectModel;
using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Syncfusion.Maui.DataGrid;

namespace felix1.AdminSection
{
    public partial class OrderDetailsVisual : ContentView
    {
        private readonly int _orderId;
        private Order _currentOrder;
        public ObservableCollection<OrderItem> OrderItems { get; } = new();
        public ObservableCollection<OrderItem> RefundedItems { get; } = new();
        public ObservableCollection<OrderItemDifference> DifferenceItems { get; } = new();

        public class OrderItemDifference : INotifyPropertyChanged
        {
            public string ArticleName { get; set; } = "";
            public int OriginalQuantity { get; set; }
            public int RefundedQuantity { get; set; }
            public int NewQuantity => OriginalQuantity - RefundedQuantity;
            public decimal UnitPrice { get; set; }
            public decimal Difference => RefundedQuantity * UnitPrice;

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public OrderDetailsVisual(int orderId)
        {
            InitializeComponent();
            _orderId = orderId;
            LoadOrderData();
            BindingContext = this;
        }

        private async void LoadOrderData()
        {
            await AppDbContext.ExecuteSafeAsync(async db =>
            {
                _currentOrder = await db.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Article)
                    .Include(o => o.Waiter)
                    .Include(o => o.Table)
                    .Include(o => o.CashRegister)
                    .FirstOrDefaultAsync(o => o.Id == _orderId);

                if (_currentOrder != null)
                {
                    var transaction = await db.Transactions
                        .FirstOrDefaultAsync(t => t.Order != null && t.Order.Id == _orderId);

                    var refund = await db.Refunds
                        .Include(r => r.RefundedItems)
                            .ThenInclude(ri => ri.Article)
                        .FirstOrDefaultAsync(r => r.Order != null && r.Order.Id == _orderId);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        orderNumberLabel.Text = $"Orden #{_currentOrder.OrderNumber:0000}";
                        dateLabel.Text = _currentOrder.Date?.ToString("dd/MM/yyyy HH:mm");
                        tableLabel.Text = _currentOrder.Table != null ?
                            $"Mesa {_currentOrder.Table.LocalNumber}" : "Sin mesa";
                        waiterLabel.Text = _currentOrder.Waiter?.Name ?? "Sin mesero";
                        discountLabel.Text = _currentOrder.Discount.ToString("C");
                        statusLabel.Text = _currentOrder.IsDuePaid ? "Pagada" : "Pendiente";

                        OrderItems.Clear();
                        if (_currentOrder.Items != null)
                        {
                            foreach (var item in _currentOrder.Items)
                            {
                                OrderItems.Add(item);
                            }
                        }

                        if (transaction != null)
                        {
                            transactionSection.IsVisible = true;
                            totalAmountLabel.Text = transaction.TotalAmount.ToString("C");
                            itbisLabel.Text = transaction.TaxAmountITBIS.ToString("C");
                            tipLabel.Text = transaction.TaxAmountWaiters.ToString("C");
                            cashLabel.Text = transaction.CashAmount.ToString("C");
                            cardLabel.Text = transaction.CardAmount.ToString("C");
                            transferLabel.Text = transaction.TransferAmount.ToString("C");
                        }
                        else
                        {
                            transactionSection.IsVisible = false;
                        }

                        if (refund != null)
                        {
                            refundSection.IsVisible = true;
                            refundReasonLabel.Text = refund.Reason ?? "Sin motivo especificado";
                            refundDateLabel.Text = refund.Date.ToString("dd/MM/yyyy HH:mm");

                            RefundedItems.Clear();
                            if (refund.RefundedItems != null)
                            {
                                foreach (var item in refund.RefundedItems)
                                {
                                    RefundedItems.Add(item);
                                }
                            }

                            CalculateDifferences();
                        }
                        else
                        {
                            refundSection.IsVisible = false;
                            differenceSection.IsVisible = false;
                        }
                    });
                }
            });
        }

        private void CalculateDifferences()
        {
            differenceSection.IsVisible = true;
            DifferenceItems.Clear();

            decimal originalTotal = 0;
            decimal refundedTotal = 0;

            foreach (var item in OrderItems)
            {
                originalTotal += item.TotalPrice;
            }

            foreach (var originalItem in OrderItems)
            {
                var refundedItem = RefundedItems.FirstOrDefault(ri => ri.Article?.Id == originalItem.Article?.Id);
                int refundedQty = refundedItem?.Quantity ?? 0;

                if (refundedQty > 0)
                {
                    var difference = new OrderItemDifference
                    {
                        ArticleName = originalItem.ArticleName,
                        OriginalQuantity = originalItem.Quantity,
                        RefundedQuantity = refundedQty,
                        UnitPrice = originalItem.UnitPrice
                    };

                    DifferenceItems.Add(difference);
                    refundedTotal += difference.Difference;
                }
            }

            decimal newTotal = originalTotal - refundedTotal;

            originalTotalLabel.Text = originalTotal.ToString("C");
            refundedTotalLabel.Text = refundedTotal.ToString("C");
            newTotalLabel.Text = newTotal.ToString("C");
            differenceLabel.Text = refundedTotal.ToString("C");
        }

        private void OnBackButtonClicked(object sender, EventArgs e)
        {
            if (Application.Current?.MainPage is NavigationPage navPage &&
                navPage.CurrentPage is AdminSectionMainVisual adminPage)
            {
                if (_currentOrder?.CashRegister != null)
                {
                    adminPage.SetRightPanelContent(new ListPaymentVisual(_currentOrder.CashRegister));
                }
                else
                {
                    adminPage.SetRightPanelContent(new ListCashRegisterVisual());
                }
            }
        }
    }
}