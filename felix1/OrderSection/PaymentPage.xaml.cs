using felix1.Data;
using felix1.Logic;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace felix1.OrderSection
{
    public partial class PaymentPage : ContentPage
    {
        public Order Order { get; set; }
        public decimal Subtotal => Order.Items?.Sum(i => i.TotalPrice) ?? 0;
        public decimal Tax => Subtotal * 0.05m;
        public decimal Total => Subtotal + Tax;
        public decimal TotalPayment => _cashAmount + _cardAmount + _transferAmount;

        public bool AnyPaymentMethodUsed => _cashAmount > 0 || _cardAmount > 0 || _transferAmount > 0;
        public bool IsCashUsed => _cashAmount > 0;
        public bool IsCardUsed => _cardAmount > 0;
        public bool IsTransferUsed => _transferAmount > 0;

        public decimal CashAmount => _cashAmount;
        public decimal CardAmount => _cardAmount;
        public decimal TransferAmount => _transferAmount;
        public decimal ChangeAmount => _changeAmount;

        private decimal _cashAmount;
        private decimal _cardAmount;
        private decimal _transferAmount;
        private decimal _changeAmount;
        private List<Frame> _activePaymentFrames = new List<Frame>();

        public PaymentPage(Order order)
        {
            InitializeComponent();
            Order = order;
            BindingContext = this;

            // Add Cash payment method by default
            AddCashMethod();
        }

        private void AddCashMethod()
        {
            var cashFrame = CreateCashFrame();
            ActivePaymentMethodsContainer.Children.Add(cashFrame);
            _activePaymentFrames.Add(cashFrame);
        }

        private void OnAddTransferClicked(object sender, EventArgs e)
        {
            if (_activePaymentFrames.Any(f => GetPaymentMethodName(f) == "Transferencia"))
            {
                DisplayAlert("Advertencia", "Ya has añadido un método de pago por transferencia", "OK");
                return;
            }

            var transferFrame = CreateTransferFrame();
            ActivePaymentMethodsContainer.Children.Add(transferFrame);
            _activePaymentFrames.Add(transferFrame);
        }

        private void OnAddCardClicked(object sender, EventArgs e)
        {
            if (_activePaymentFrames.Any(f => GetPaymentMethodName(f) == "Tarjeta"))
            {
                DisplayAlert("Advertencia", "Ya has añadido un método de pago por tarjeta", "OK");
                return;
            }

            var cardFrame = CreateCardFrame();
            ActivePaymentMethodsContainer.Children.Add(cardFrame);
            _activePaymentFrames.Add(cardFrame);
        }

        private string GetPaymentMethodName(Frame frame)
        {
            if (frame.Content is VerticalStackLayout layout &&
                layout.Children[0] is Grid header &&
                header.Children[0] is Label label)
            {
                return label.Text;
            }
            return null;
        }

        private Frame CreateCashFrame()
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#F5F5F5"),
                BorderColor = Colors.White,
                CornerRadius = 10,
                Padding = 10,
                HasShadow = true
            };

            var layout = new VerticalStackLayout();

            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star }
                }
            };

            var label = new Label
            {
                Text = "Efectivo",
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#005F8C")
            };
            Grid.SetColumn(label, 0);
            header.Children.Add(label);

            layout.Children.Add(header);

            var entry = new Entry
            {
                Placeholder = "$0.00",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Colors.White,
                TextColor = Colors.Black
            };

            entry.TextChanged += (sender, e) =>
            {
                if (decimal.TryParse(e.NewTextValue, out var amount))
                    _cashAmount = amount;
                else
                    _cashAmount = 0;

                UpdatePaymentSummary();
                UpdateProperties();
            };

            layout.Children.Add(entry);
            frame.Content = layout;

            return frame;
        }

        private Frame CreateCardFrame()
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#F5F5F5"),
                BorderColor = Colors.White,
                CornerRadius = 10,
                Padding = 10,
                HasShadow = true
            };

            var layout = new VerticalStackLayout();

            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var label = new Label
            {
                Text = "Tarjeta",
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#005F8C")
            };
            Grid.SetColumn(label, 0);
            header.Children.Add(label);

            var removeButton = new Button
            {
                Text = "×",
                FontSize = 20,
                TextColor = Color.FromArgb("#FF0000"),
                BackgroundColor = Colors.Transparent,
                Padding = 0,
                WidthRequest = 30,
                HeightRequest = 30,
                CornerRadius = 15
            };

            removeButton.Clicked += (s, e) =>
            {
                ActivePaymentMethodsContainer.Children.Remove(frame);
                _activePaymentFrames.Remove(frame);
                _cardAmount = 0;
                UpdatePaymentSummary();
                UpdateProperties();
            };

            Grid.SetColumn(removeButton, 1);
            header.Children.Add(removeButton);
            layout.Children.Add(header);

            var entry = new Entry
            {
                Placeholder = "$0.00",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Colors.White,
                TextColor = Colors.Black
            };

            entry.TextChanged += (sender, e) =>
            {
                if (decimal.TryParse(e.NewTextValue, out var amount))
                    _cardAmount = amount;
                else
                    _cardAmount = 0;
                UpdatePaymentSummary();
                UpdateProperties();
            };

            layout.Children.Add(entry);
            frame.Content = layout;

            return frame;
        }

        private Frame CreateTransferFrame()
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#F5F5F5"),
                BorderColor = Colors.White,
                CornerRadius = 10,
                Padding = 10,
                HasShadow = true
            };

            var layout = new VerticalStackLayout();

            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var label = new Label
            {
                Text = "Transferencia",
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#005F8C")
            };
            Grid.SetColumn(label, 0);
            header.Children.Add(label);

            var removeButton = new Button
            {
                Text = "×",
                FontSize = 20,
                TextColor = Color.FromArgb("#FF0000"),
                BackgroundColor = Colors.Transparent,
                Padding = 0,
                WidthRequest = 30,
                HeightRequest = 30,
                CornerRadius = 15
            };

            removeButton.Clicked += (s, e) =>
            {
                ActivePaymentMethodsContainer.Children.Remove(frame);
                _activePaymentFrames.Remove(frame);
                _transferAmount = 0;
                UpdatePaymentSummary();
                UpdateProperties();
            };

            Grid.SetColumn(removeButton, 1);
            header.Children.Add(removeButton);
            layout.Children.Add(header);

            var entry = new Entry
            {
                Placeholder = "$0.00",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Colors.White,
                TextColor = Colors.Black
            };

            entry.TextChanged += (sender, e) =>
            {
                if (decimal.TryParse(e.NewTextValue, out var amount))
                    _transferAmount = amount;
                else
                    _transferAmount = 0;
                UpdatePaymentSummary();
                UpdateProperties();
            };

            layout.Children.Add(entry);
            frame.Content = layout;

            return frame;
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
            UpdateProperties();

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
                    _changeAmount = change;
                    OnPropertyChanged(nameof(ChangeAmount));
                }
            }
            else
            {
                PaymentSummaryLabel.TextColor = Colors.Red;
            }
        }

        private void UpdateProperties()
        {
            OnPropertyChanged(nameof(AnyPaymentMethodUsed));
            OnPropertyChanged(nameof(IsCashUsed));
            OnPropertyChanged(nameof(IsCardUsed));
            OnPropertyChanged(nameof(IsTransferUsed));
            OnPropertyChanged(nameof(CashAmount));
            OnPropertyChanged(nameof(CardAmount));
            OnPropertyChanged(nameof(TransferAmount));
            OnPropertyChanged(nameof(ChangeAmount));
            OnPropertyChanged(nameof(TotalPayment));
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Color.FromArgb("#DFF6FF") : Color.FromArgb("#FFFFFF");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}