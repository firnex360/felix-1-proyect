using felix1.Data;
using felix1.Logic;
using Microsoft.Maui.ApplicationModel;

#if WINDOWS
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
#endif


namespace felix1.OrderSection;

public partial class OrderSectionMainVisual : ContentPage
{
    private CashRegister _cashRegister;

    public OrderSectionMainVisual(CashRegister cashRegister)
    {
        InitializeComponent();
        _cashRegister = cashRegister;
        DisplayCashRegisterInfo();
        RightPanel.Content = new ListOrderVisual();

#if WINDOWS
        var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
        if (window != null)
        {
            WindowUtils.MaximizeWindow(window);
        }
#endif

    }

    private void DisplayCashRegisterInfo()
    {
        lblCashRegisterInfo.Text = $"Caja #{_cashRegister.Number} " /*+
                                 $"Abierta por: {_cashRegister.Cashier?.Name}\n" +
                                 $"Hora de apertura: {_cashRegister.TimeStarted:dd/MM/yyyy HH:mm}\n"*/;
    }

    private async void OnCloseRegister(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmación",
            $"¿Cerrar la caja?",
            "Sí",
            "No");

        if (!confirm)
            return;

        await AppDbContext.ExecuteSafeAsync(async db =>
        {
            var user = await db.Users.FindAsync(AppSession.CurrentUser.Id);
            var register = await db.CashRegisters.FindAsync(_cashRegister.Id);

            if (register != null)
            {
                register.Cashier = user;
                register.IsOpen = false;
                register.TimeFinish = DateTime.Now;
                db.CashRegisters.Update(register);
                await db.SaveChangesAsync();
            }

            Dispatcher.Dispatch(() =>
            {
                /*Navigation.PopAsync();
                var balanceVisual = new BalanceVisual(register);
                Application.Current!.MainPage = new NavigationPage(balanceVisual);*/
                Navigation.PopAsync();
                var balanceVisual = new LoginPage();
                Microsoft.Maui.Controls.Application.Current!.MainPage = new NavigationPage(balanceVisual);
            });
        });
    }

    private async void OnCloseSesion(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmación",
            "¿Estás seguro que deseas cerrar sesión?",
            "Sí",
            "No");

        if (!confirm)
            return;

        AppSession.CurrentUser = null!;

        var loginPage = new LoginPage();
        Microsoft.Maui.Controls.Application.Current!.MainPage = new NavigationPage(loginPage);
    }

    [Obsolete("This method is obsolete, delete this if sure wont be of use in the future")]
    private async void OnPaymentButtonClicked(object sender, EventArgs e)
    {
        // Crear un pedido de prueba con artículos reales
        var testOrder = new Order
        {
            Id = 999,
            Date = DateTime.Now,
            Items = new List<OrderItem>
        {
            new OrderItem
            {
                Article = new Article { Name = "Pollo Frito" },
                Quantity = 1,
                UnitPrice = 250.50m
            },
            new OrderItem
            {
                Article = new Article { Name = "Sandía" },
                Quantity = 2,
                UnitPrice = 22.25m
            }
        }
        };

        // Mostrar pantalla de pago
        var paymentPage = new PaymentVisual(testOrder);

        if (Navigation != null)
        {
            await Navigation.PushAsync(paymentPage);
        }
        else
        {
            await Microsoft.Maui.Controls.Application.Current!.MainPage!.DisplayAlert("Error", "No se puede navegar a la página de pago", "OK");
        }
    }

    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        // Get the ListOrderVisual instance and pass the search text to it
        if (RightPanel.Content is ListOrderVisual listOrderVisual)
        {
            // Call the filter method to highlight matching table numbers
            listOrderVisual.FilterTablesByNumber(searchText);
        }
    }

    private void OnSearchBarSearchButtonPressed(object sender, EventArgs e)
    {
        // Handle search button press - open the highlighted table
        if (RightPanel.Content is ListOrderVisual listOrderVisual)
        {
            listOrderVisual.OpenHighlightedTable();
        }
    }

    public void FocusSearchBar()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(100); // Give time for the window to close
            searchBar?.Focus(); // Focus the search bar
            searchBar?.ClearValue(Entry.TextProperty); // Clear the search bar text
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        this.HandlerChanged += OnHandlerChanged;
        OnHandlerChanged(this, EventArgs.Empty);
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        if (searchBar?.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.AutoSuggestBox autoSuggestBox)
        {
            autoSuggestBox.KeyDown -= SearchBarPlatformView_KeyDown;
            autoSuggestBox.KeyDown += SearchBarPlatformView_KeyDown;
        }
#endif
    }

#if WINDOWS
    private void SearchBarPlatformView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Tab)
        {
            if (RightPanel.Content is ListOrderVisual listOrderVisual)
            {
                listOrderVisual.MoveToNextMatchingTable();
                e.Handled = true;
            }
        }
        else if (e.Key == Windows.System.VirtualKey.Enter)
        {
            OnSearchBarSearchButtonPressed(sender, EventArgs.Empty);
            e.Handled = true;
        }
    }
#endif

        private void OnShowCompletedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (RightPanel.Content is ListOrderVisual listOrderVisual)
        {
            listOrderVisual.ShowCompletedOrders = e.Value;
            listOrderVisual.ReloadTM();
        }
    }

}