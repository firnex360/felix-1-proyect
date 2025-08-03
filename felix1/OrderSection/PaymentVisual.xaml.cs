using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Scriban;

#if WINDOWS
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using System.Drawing;
using System.Drawing.Printing;
#endif

namespace felix1.OrderSection
{
    public partial class PaymentVisual : ContentPage
    {
        private decimal _taxRate = 0.0m;
        private decimal _waiterTaxRate = 0.0m;
        private decimal _deliveryTaxRate = 0.0m;

        public Order Order { get; set; }
        public decimal Subtotal => Order.Items?.Sum(i => i.TotalPrice) ?? 0;
        public decimal Discount => Order.Discount;
        public decimal TaxableAmount => Subtotal - Discount;
        public decimal TaxDelivery => Subtotal * _deliveryTaxRate;
        public decimal TaxITBIS => Subtotal * _taxRate;
        public decimal TaxWaiters => Subtotal * _waiterTaxRate;
        public decimal Total => Order.Table!.IsTakeOut ? TotalDelivery : TotalCasual;
        public decimal TotalCasual => (Subtotal + TaxITBIS + TaxWaiters) - Discount;
        public decimal TotalDelivery => (Subtotal + TaxDelivery) - Discount;
        public decimal TotalPayment => _cashAmount + _cardAmount + _transferAmount;
        public int ItemsCount => Order.Items?.Sum(i => i.Quantity) ?? 0;
        public string OrderInfoText { get; set; }
        public string TaxDeliveryLabel => $"Delivery ({_deliveryTaxRate:P0})";
        public string TaxITBISLabel => $"ITBIS ({_taxRate:P0})";
        public string TaxWaitersLabel => $"Propina ({_waiterTaxRate:P0})";
        public bool ShowDeliveryTax => Order.Table?.IsTakeOut ?? false;
        public bool ShowITBISAndWaiterTax => !ShowDeliveryTax;


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
        private Order? _order;
        private Transaction? _transaction;
        private List<Frame> _activePaymentFrames = new List<Frame>();

        private int _currentPaymentIndex = 0;
        private bool _isPaymentFocused = false;
        private Entry? _currentFocusedEntry;

        public PaymentVisual(Order order)
        {
            LoadTaxRatesFromConfiguration();
            InitializeComponent();
            Order = order;
            BindingContext = this;

            OrderInfoText = $"Mesa {order.Table?.LocalNumber} ({order.Table?.GlobalNumber}) - Orden #{order.Id}";

            UpdatePaymentSummary();
            AddCashMethod();
            FocusFirstPaymentEntry();

            btnCancel.Focused += (s, e) =>
            {
                _isPaymentFocused = false;
                _currentFocusedEntry = null!;
            };

            btnProcessPayment.Focused += (s, e) =>
            {
                _isPaymentFocused = false;
                _currentFocusedEntry = null!;
            };
        }

        private void LoadTaxRatesFromConfiguration()
        {
            _taxRate = decimal.Parse(Preferences.Get("TaxRate", "18")) / 100m;
            _waiterTaxRate = decimal.Parse(Preferences.Get("WaiterTaxRate", "10")) / 100m;
            _deliveryTaxRate = decimal.Parse(Preferences.Get("DeliveryTaxRate", "0")) / 100m;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            this.HandlerChanged += OnHandlerChanged;
            OnHandlerChanged(this, EventArgs.Empty);
            FocusFirstPaymentEntry();
        }

        private void OnHandlerChanged(object? sender, EventArgs e)
        {
#if WINDOWS
            var platformView = this.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
            if (platformView != null)
            {
                platformView.KeyDown += PlatformView_KeyDown;
            }
#endif
        }

#if WINDOWS
        private void PlatformView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {                    
                case Windows.System.VirtualKey.Enter:
                    if (_currentFocusedEntry != null)
                    {
                        NavigateToNextControl();
                    }
                    else
                    {
                        OnChargeClicked(this, null);
                    }
                    e.Handled = true;
                    break;
                    
                case Windows.System.VirtualKey.Tab:
                    NavigateToNextControl();
                    e.Handled = true;
                    break;

                
                case Windows.System.VirtualKey.F3:
                    ShowRemainingAmount();
                    e.Handled = true;
                    break;
                    
                case Windows.System.VirtualKey.F6:
                    OnAddTransferClicked(sender, null);
                    e.Handled = true;
                    break;
                
                case Windows.System.VirtualKey.F7:
                    OnAddCardClicked(sender, null);
                    e.Handled = true;
                    break;

                case Windows.System.VirtualKey.F4:
                    OnChargeClicked(this, null);
                    e.Handled = true;
                    break;
                
                case Windows.System.VirtualKey.F1:
                    OnCancelButtonClicked(this, null);
                    e.Handled = true;
                    break;

                case Windows.System.VirtualKey.Escape:
                    CloseThisWindow();
                    e.Handled = true;
                    break;
            }
        }
#endif

        private void FocusFirstPaymentEntry()
        {
            if (ActivePaymentMethodsContainer.Children.Count > 0)
            {
                if (ActivePaymentMethodsContainer.Children[0] is Frame frame &&
                    frame.Content is VerticalStackLayout layout &&
                    layout.Children[1] is Entry entry)
                {
                    Dispatcher.Dispatch(async () =>
                    {
                        await Task.Delay(300);
                        entry.Focus();
                        entry.CursorPosition = 0;
                        entry.SelectionLength = entry.Text?.Length ?? 0;
                        _currentFocusedEntry = entry;
                        _isPaymentFocused = true;
                        _currentPaymentIndex = 0;
                    });
                }
            }
        }

        private void ShowRemainingAmount()
        {
            if (_currentFocusedEntry == null) return;

            decimal remaining = Total - TotalPayment;
            if (remaining > 0)
            {
                _currentFocusedEntry.Text = remaining.ToString("N2");
                _currentFocusedEntry.CursorPosition = 0;
                _currentFocusedEntry.SelectionLength = _currentFocusedEntry.Text.Length;

                if (_currentFocusedEntry.Parent.Parent is Frame frame)
                {
                    string? method = GetPaymentMethodName(frame);
                    switch (method)
                    {
                        case "Efectivo":
                            _cashAmount = remaining;
                            break;
                        case "Tarjeta":
                            _cardAmount = remaining;
                            break;
                        case "Transferencia":
                            _transferAmount = remaining;
                            break;
                    }
                    UpdatePaymentSummary();
                    UpdateProperties();
                }
            }
            else
            {
                DisplayAlert("Información", "La orden ya está completamente pagada", "OK");
            }
        }


        private void NavigateToNextControl()
        {
            if (_isPaymentFocused && _currentFocusedEntry != null)
            {
                _currentPaymentIndex++;
                if (_currentPaymentIndex >= ActivePaymentMethodsContainer.Children.Count)
                {
                    _isPaymentFocused = false;
                    _currentFocusedEntry = null!;
                    btnCancel.Focus();
                    return;
                }

                if (ActivePaymentMethodsContainer.Children[_currentPaymentIndex] is Frame frame &&
                    frame.Content is VerticalStackLayout layout &&
                    layout.Children[1] is Entry entry)
                {
                    entry.Focus();
                    _currentFocusedEntry = entry;
                }
            }
            else if (btnCancel.IsFocused)
            {
                btnProcessPayment.Focus();
            }
            else if (btnProcessPayment.IsFocused)
            {
                FocusFirstPaymentEntry();
            }
            else
            {
                FocusFirstPaymentEntry();
            }
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

            if (transferFrame.Content is VerticalStackLayout layout && layout.Children[1] is Entry entry)
            {
                entry.Focus();
                _currentFocusedEntry = entry;
                _currentPaymentIndex = ActivePaymentMethodsContainer.Children.Count - 1;
            }
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

            if (cardFrame.Content is VerticalStackLayout layout && layout.Children[1] is Entry entry)
            {
                entry.Focus();
                _currentFocusedEntry = entry;
                _currentPaymentIndex = ActivePaymentMethodsContainer.Children.Count - 1;
            }
        }

        private string? GetPaymentMethodName(Frame frame)
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
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"),
                BorderColor = Microsoft.Maui.Graphics.Color.FromArgb("#FFFFFF"),
                CornerRadius = 10,
                Padding = 10,
                HasShadow = true
            };

            var layout = new VerticalStackLayout();

            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = Microsoft.Maui.GridLength.Star }
                }
            };

            var label = new Label
            {
                Text = "Efectivo",
                FontAttributes = FontAttributes.Bold,
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#005F8C"),
                FontFamily = "Inter"
            };
            Grid.SetColumn(label, 0);
            header.Children.Add(label);

            layout.Children.Add(header);

            var entry = new Entry
            {
                Placeholder = "$0.00",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#FFFFFF"),
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#000000"),
                FontFamily = "Inter"
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

            entry.Focused += (sender, e) =>
            {
                _currentFocusedEntry = entry;
                _isPaymentFocused = true;
            };

            layout.Children.Add(entry);
            frame.Content = layout;

            return frame;
        }

        private Frame CreateCardFrame()
        {
            var frame = new Frame
            {
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"),
                BorderColor = Microsoft.Maui.Graphics.Color.FromArgb("#FFFFFF"),
                CornerRadius = 10,
                Padding = 10,
                HasShadow = true
            };

            var layout = new VerticalStackLayout();

            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = Microsoft.Maui.GridLength.Star },
                    new ColumnDefinition { Width = Microsoft.Maui.GridLength.Auto }
                }
            };

            var label = new Label
            {
                Text = "Tarjeta",
                FontAttributes = FontAttributes.Bold,
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#005F8C"),
                FontFamily = "Inter"
            };
            Grid.SetColumn(label, 0);
            header.Children.Add(label);

            var removeButton = new Button
            {
                Text = "x",
                FontSize = 20,
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#FD2D2D"),
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#00000000"),
                Padding = 0,
                FontFamily = "Inter",
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

                if (_currentFocusedEntry != null && _currentFocusedEntry == (layout.Children[1] as Entry))
                {
                    _currentFocusedEntry = null!;
                    FocusFirstPaymentEntry();
                }
            };

            Grid.SetColumn(removeButton, 1);
            header.Children.Add(removeButton);
            layout.Children.Add(header);

            var entry = new Entry
            {
                Placeholder = "$0.00",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#FFFFFF"),
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#000000"),
                FontFamily = "Inter"
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

            entry.Focused += (sender, e) =>
            {
                _currentFocusedEntry = entry;
                _isPaymentFocused = true;
            };

            layout.Children.Add(entry);
            frame.Content = layout;

            return frame;
        }

        private Frame CreateTransferFrame()
        {
            var frame = new Frame
            {
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#F5F5F5"),
                BorderColor = Microsoft.Maui.Graphics.Color.FromArgb("#FFFFFF"),
                CornerRadius = 10,
                Padding = 10,
                HasShadow = true
            };

            var layout = new VerticalStackLayout();

            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = Microsoft.Maui.GridLength.Star },
                    new ColumnDefinition { Width = Microsoft.Maui.GridLength.Auto }
                }
            };

            var label = new Label
            {
                Text = "Transferencia",
                FontAttributes = FontAttributes.Bold,
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#005F8C"),
                FontFamily = "Inter"
            };
            Grid.SetColumn(label, 0);
            header.Children.Add(label);

            var removeButton = new Button
            {
                Text = "x",
                FontSize = 20,
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#FD2D2D"),
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#00000000"),
                FontFamily = "Inter",
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

                if (_currentFocusedEntry != null && _currentFocusedEntry == (layout.Children[1] as Entry))
                {
                    _currentFocusedEntry = null!;
                    FocusFirstPaymentEntry();
                }
            };

            Grid.SetColumn(removeButton, 1);
            header.Children.Add(removeButton);
            layout.Children.Add(header);

            var entry = new Entry
            {
                Placeholder = "$0.00",
                Keyboard = Keyboard.Numeric,
                BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#FFFFFF"),
                TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#000000"),
                FontFamily = "Inter"
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

            entry.Focused += (sender, e) =>
            {
                _currentFocusedEntry = entry;
                _isPaymentFocused = true;
            };

            layout.Children.Add(entry);
            frame.Content = layout;

            return frame;
        }

        private async void OnCancelButtonClicked(object sender, EventArgs e)
        {
            Order.IsBillRequested = false;

            await AppDbContext.ExecuteSafeAsync(async db =>
            {
                var orderToUpdate = await db.Orders.FirstOrDefaultAsync(o => o.Id == Order.Id);
                if (orderToUpdate != null)
                {
                    orderToUpdate.IsBillRequested = false;
                    await db.SaveChangesAsync();
                }
                return true;
            });

            CloseThisWindow();
            ListTableVisual.Instance?.ReloadTM();
        }

        private async void OnChargeClicked(object sender, EventArgs e)
        {
            bool fromKeyboard = sender == null || sender is not Button;

            var totalPayment = _cashAmount + _cardAmount + _transferAmount;

            if (!Order.IsDuePaid)
            {
                if (totalPayment < Total)
                {
                    await DisplayAlert("Error", $"El pago (${totalPayment:N2}) no cubre el total de la orden (${Total:N2})", "OK");
                    return;
                }
            }

            _changeAmount = totalPayment > Total ? totalPayment - Total : 0;
            UpdateProperties();

            bool transactionSuccess = await AppDbContext.ExecuteSafeAsync(async db =>
            {
                var orderToUpdate = await db.Orders
                    .Include(o => o.Table)
                    .Include(o => o.Items)
                    .Include(o => o.CashRegister)
                        .ThenInclude(cr => cr!.Cashier)
                    .FirstOrDefaultAsync(o => o.Id == Order.Id);

                if (orderToUpdate == null) return false;

                var transaction = new Transaction
                {
                    Date = DateTime.Now,
                    Order = orderToUpdate,
                    TotalAmount = Total,
                    TaxAmountITBIS = TaxITBIS,
                    TaxAmountWaiters = TaxWaiters,
                    CashAmount = _cashAmount,
                    CardAmount = _cardAmount,
                    TransferAmount = _transferAmount
                };

                _transaction = transaction;

                if (Order.IsDuePaid)
                {
                    orderToUpdate.IsDuePaid = true;

                    if (orderToUpdate.Table != null)
                    {
                        orderToUpdate.Table.IsPaid = true;
                        orderToUpdate.Table.IsBillRequested = false;
                    }

                    if (orderToUpdate.IsDuePaid)
                    {
                        await DisplayAlert("Información",
                            $"Pago registrado (${totalPayment:N2}) para cuenta por cobrar",
                            "OK");
                    }
                }

                db.Transactions.Add(transaction);
                await db.SaveChangesAsync();

                if (Order.Table != null)
                {
                    Order.Table.IsPaid = true;
                    Order.Table.IsBillRequested = false;
                }

                return true;
            });

            if (!transactionSuccess)
            {
                await DisplayAlert("Error", "No se pudo guardar la transacción", "OK");
                return;
            }

            OnPrintReceipt(this, null!);
            CloseThisWindow();
            ListTableVisual.Instance?.ReloadTM();
        }

        private void CloseThisWindow()
        {
            var app = Microsoft.Maui.Controls.Application.Current;
            if (app != null)
            {
                foreach (var window in app.Windows)
                {
                    if (window.Page == this)
                    {
                        app.CloseWindow(window);
                        FocusOrderSectionSearchBar();
                        break;
                    }
                }
            }
        }

        private void FocusOrderSectionSearchBar()
        {
            var app = Microsoft.Maui.Controls.Application.Current;
            if (app != null)
            {
                foreach (var window in app.Windows)
                {
                    if (window.Page is OrderSectionMainVisual orderSectionPage)
                    {
                        orderSectionPage.FocusSearchBar();
                        break;
                    }
                    else if (window.Page is NavigationPage navPage && navPage.CurrentPage is OrderSectionMainVisual orderSectionMainPage)
                    {
                        orderSectionMainPage.FocusSearchBar();
                        break;
                    }
                }
            }
        }

        private void UpdatePaymentSummary()
        {
            var totalPayment = _cashAmount + _cardAmount + _transferAmount;
            PaymentSummaryLabel.Text = $"Pagado: ${totalPayment:N2} / ${Total:N2}";

            if (totalPayment >= Total)
            {
                PaymentSummaryLabel.TextColor = Colors.Green;

                if (totalPayment > Total)
                {
                    var change = totalPayment - Total;
                    PaymentSummaryLabel.Text += $" (Devuelta: ${change:N2})";
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
            var totalPayment = _cashAmount + _cardAmount + _transferAmount;
            _changeAmount = totalPayment > Total ? totalPayment - Total : 0;

            OnPropertyChanged(nameof(ChangeAmount));
            OnPropertyChanged(nameof(AnyPaymentMethodUsed));
            OnPropertyChanged(nameof(IsCashUsed));
            OnPropertyChanged(nameof(IsCardUsed));
            OnPropertyChanged(nameof(IsTransferUsed));
            OnPropertyChanged(nameof(CashAmount));
            OnPropertyChanged(nameof(CardAmount));
            OnPropertyChanged(nameof(TransferAmount));
            OnPropertyChanged(nameof(TotalPayment));
            OnPropertyChanged(nameof(ItemsCount));
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Discount));
            OnPropertyChanged(nameof(TaxableAmount));
            OnPropertyChanged(nameof(TaxITBIS));
            OnPropertyChanged(nameof(TaxWaiters));
            OnPropertyChanged(nameof(TaxITBISLabel));
            OnPropertyChanged(nameof(TaxWaitersLabel));
            OnPropertyChanged(nameof(Total));
        }

        // Print receipt method (for transaction)
        private void OnPrintReceipt(object sender, EventArgs e)
        {
            Console.WriteLine("Print transaction receipt clicked");

            // Create a transaction object for the receipt
            var transaction = new Transaction
            {
                Date = DateTime.Now,
                Order = Order,
                TotalAmount = Total,
                TaxAmountITBIS = TaxITBIS,
                TaxAmountWaiters = TaxWaiters,
                CashAmount = _cashAmount,
                CardAmount = _cardAmount,
                TransferAmount = _transferAmount
            };


#if WINDOWS
            try
            {

                var orderReceiptLocal = new OrderReceipt
                {
                    // Company Information (loaded from configuration/settings)
                    CompanyName = Preferences.Get("CompanyName", "Sin nombre"),
                    CompanyAddress = Preferences.Get("CompanyAddress", "Sin dirección"),
                    CompanyPhone = Preferences.Get("CompanyPhone", "0"),
                    CompanyRNC = Preferences.Get("CompanyRNC", "0"),

                    // Order Information
                    Order = Order,

                    // Transaction Financial Information
                    TotalAmount = Total,
                    TaxAmountITBIS = TaxITBIS,
                    TaxAmountWaiters = TaxWaiters,
                    TaxAmountDelivery = TaxDelivery,
                    CashAmount = _cashAmount,
                    CardAmount = _cardAmount,
                    TransferAmount = _transferAmount,

                    // Tax Percentages
                    TaxRateITBIS = _taxRate, // Store the actual tax rate (e.g., 0.18 for 18%)
                    TaxRateWaiters = _waiterTaxRate, // Store the actual waiter tax rate (e.g., 0.10 for 10%)
                    TaxRateDelivery = _deliveryTaxRate, // Store the actual delivery tax rate (e.g., 0.10 for 10%)

                    PrintDate = DateTime.Now
                };

                // Create PaymentReceipt object with all necessary data
                var paymentReceipt = new PaymentReceipt
                {
                    orderReceipt = orderReceiptLocal,
                    transaction = _transaction,
                    PrintDate = DateTime.Now
                };

                if (paymentReceipt.orderReceipt.Order!.Table!.IsTakeOut)
                {
                    paymentReceipt.orderReceipt.Order.Waiter = paymentReceipt.orderReceipt.Order!.CashRegister!.Cashier;
                    paymentReceipt.orderReceipt.TaxAmountITBIS = 0;
                    paymentReceipt.orderReceipt.TaxAmountWaiters = 0;
                }
                else
                {
                    paymentReceipt.orderReceipt.TaxAmountDelivery = 0;
                }

                string templateText = File.ReadAllText(@"felix1\ReceiptTemplates\PaymentTemplate.txt");
                var template = Template.Parse(templateText);
                var scribanModel = new { transaction = paymentReceipt};
                string text = template.Render(scribanModel, member => member.Name);
                System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument();
                var savedPrinter = Preferences.Get("SelectedPrinter", "");
                if (!string.IsNullOrEmpty(savedPrinter))
                {
                    pd.PrinterSettings.PrinterName = savedPrinter; // or whatever name shows in Windows, but it should take the default one
                }
                pd.PrintPage += (sender, e) =>
                {
                    System.Drawing.Font font = new System.Drawing.Font("Consolas", 10);
                    e.Graphics!.DrawString(text, font, System.Drawing.Brushes.Black, new System.Drawing.PointF(10, 10));
                };
                pd.Print();
                Console.WriteLine("Printed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Print failed: " + ex.Message);
            }
#else
            Console.WriteLine("Printing is only supported on Windows for now.");
            DisplayAlert("Error", $"No se pudo imprimir el recibo: La funcionalidad de impresión no está disponible en esta plataforma (solo Windows).", "OK");
#endif
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return (value is bool b && b)
                ? Microsoft.Maui.Graphics.Color.FromArgb("#DFF6FF")
                : Microsoft.Maui.Graphics.Color.FromArgb("#FFFFFF");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}