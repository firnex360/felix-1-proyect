using felix1.Data;
using felix1.Logic;
using Microsoft.Maui.Controls;
using System;
using System.Linq;

namespace felix1.OrderSection
{
    public partial class PaymentPage : ContentPage
    {
        public Order Order { get; set; }
        public decimal Subtotal => Order.Items?.Sum(i => i.TotalPrice) ?? 0;
        public decimal Tax => Subtotal * 0.05m;
        public decimal Total => Subtotal + Tax;

        private decimal _cashAmount;
        private decimal _cardAmount;
        private decimal _transferAmount;
        private decimal _changeAmount;

        public PaymentPage(Order order)
        {
            InitializeComponent();
            Order = order;
            BindingContext = this;
        }

        private void OnAddPaymentMethodClicked(object sender, EventArgs e)
        {
            // Toggle visibility of payment method selection
            PaymentMethodSelectionGrid.IsVisible = !PaymentMethodSelectionGrid.IsVisible;
        }

        private void OnCashSelected(object sender, EventArgs e)
        {
            CashFrame.IsVisible = true;
            PaymentMethodSelectionGrid.IsVisible = false;
            UpdateAddPaymentMethodButton();
        }

        private void OnCardSelected(object sender, EventArgs e)
        {
            CardFrame.IsVisible = true;
            PaymentMethodSelectionGrid.IsVisible = false;
            UpdateAddPaymentMethodButton();
        }

        private void OnTransferSelected(object sender, EventArgs e)
        {
            TransferFrame.IsVisible = true;
            PaymentMethodSelectionGrid.IsVisible = false;
            UpdateAddPaymentMethodButton();
        }

        private void UpdateAddPaymentMethodButton()
        {
            bool allVisible = CashFrame.IsVisible && CardFrame.IsVisible && TransferFrame.IsVisible;
            AddPaymentMethodButton.IsVisible = !allVisible;
        }

        private void OnChargeClicked(object sender, EventArgs e)
        {
            var totalPayment = _cashAmount + _cardAmount + _transferAmount;

            if (totalPayment < Total)
            {
                DisplayAlert("Error", $"El total pagado (${totalPayment:F2}) es menor que el total de la orden (${Total:F2})", "OK");
                return;
            }

            _changeAmount = totalPayment > Total ? totalPayment - Total : 0;

            var transaction = new Transaction
            {
                Date = DateTime.Now,
                Order = Order,
                TotalAmount = Total,
                TaxAmount = Tax,
                CashAmount = _cashAmount,
                CardAmount = _cardAmount,
                TransferAmount = _transferAmount
            };

            string message = $"Efectivo: ${_cashAmount:F2}\n" +
                            $"Tarjeta: ${_cardAmount:F2}\n" +
                            $"Transferencia: ${_transferAmount:F2}\n" +
                            $"Total: ${Total:F2}";

            if (_changeAmount > 0)
            {
                message += $"\n\nDevuelta: ${_changeAmount:F2}";
            }

            DisplayAlert("Pago realizado", message, "OK");

            //no se puede tener ua base de datos en está economía
        }

        private void OnCashAmountChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(e.NewTextValue, out var amount))
            {
                _cashAmount = amount;
            }
            else
            {
                _cashAmount = 0;
            }
            UpdatePaymentSummary();
        }

        private void OnCardAmountChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(e.NewTextValue, out var amount))
            {
                _cardAmount = amount;
            }
            else
            {
                _cardAmount = 0;
            }
            UpdatePaymentSummary();
        }

        private void OnTransferAmountChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(e.NewTextValue, out var amount))
            {
                _transferAmount = amount;
            }
            else
            {
                _transferAmount = 0;
            }
            UpdatePaymentSummary();
        }

        private void UpdatePaymentSummary()
        {
            var totalPayment = _cashAmount + _cardAmount + _transferAmount;
            PaymentSummaryLabel.Text = $"Pagado: ${totalPayment:F2} / ${Total:F2}";

            if (totalPayment >= Total)
            {
                PaymentSummaryLabel.TextColor = Colors.Green;

                if (totalPayment > Total)
                {
                    var change = totalPayment - Total;
                    PaymentSummaryLabel.Text += $" (Devuelta: ${change:F2})";
                }
            }
            else
            {
                PaymentSummaryLabel.TextColor = Colors.Red;
            }
        }
    }
}