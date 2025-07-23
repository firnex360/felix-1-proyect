using felix1.Data;
using felix1.Logic;
using Microsoft.Maui.ApplicationModel;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Maui.Popup;
using Application = Microsoft.Maui.Controls.Application;

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
        RightPanel.Content = new ListTableVisual();

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
                Navigation.PopAsync();
                var balanceVisual = new BalanceVisual(register!);
                Application.Current!.MainPage = new NavigationPage(balanceVisual);
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

    private async void OnPaymentButtonClicked(object sender, EventArgs e)
    {
        // esto sigue siendo relevante?
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

        // Get the ListTableVisual instance and pass the search text to it
        if (RightPanel.Content is ListTableVisual ListTableVisual)
        {
            // Call the filter method to highlight matching table numbers
            ListTableVisual.FilterTablesByNumber(searchText);
        }
    }

    private async void OnSearchBarSearchButtonPressed(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(searchBar.Text))
        {
            // Show popup with take-out section and available waiters
            await ShowQuickAccessPopup();
            return;
        }

        // Handle search button press - open the highlighted table
        if (RightPanel.Content is ListTableVisual ListTableVisual)
        {
            ListTableVisual.OpenHighlightedTable();
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
        // Handle keyboard shortcuts
        switch (e.Key)
        {
            case Windows.System.VirtualKey.F1: // F1 for Takeout
                CreateTakeoutOrder();
                e.Handled = true;
                return;
            case Windows.System.VirtualKey.F2: // F2 for New Table
                _ = ShowQuickAccessPopup();
                e.Handled = true;
                return;
        }

        if (e.Key == Windows.System.VirtualKey.Tab)
        {
            if (RightPanel.Content is ListTableVisual ListTableVisual)
            {
                ListTableVisual.MoveToNextMatchingTable();
                e.Handled = true;
            }
        }
        else if (e.Key == Windows.System.VirtualKey.Enter)
        {
            OnSearchBarSearchButtonPressed(sender, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void CreateTakeoutOrder()
    {
        if (RightPanel.Content is ListTableVisual listTableVisual)
        {
            // Call the create takeout order method directly
            var method = typeof(ListTableVisual).GetMethod("OnCreateTakeoutOrderClicked", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(listTableVisual, new object[] { this, EventArgs.Empty });
            }
        }
    }
#endif

    private void OnShowCompletedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (RightPanel.Content is ListTableVisual ListTableVisual)
        {
            ListTableVisual.ShowCompletedOrders = e.Value;
            ListTableVisual.ReloadTM();
        }
    }

    private async Task ShowQuickAccessPopup()
    {
        try
        {
            // Create the popup content
            var popupContent = new StackLayout
            {
                Spacing = 15,
                Padding = 20,
                BackgroundColor = Colors.White,
                WidthRequest = 400,
                HeightRequest = 300
            };

            // Title
            popupContent.Children.Add(new Label
            {
                Text = "🚀 Acceso Rápido",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#005F8C"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Microsoft.Maui.Thickness(0, 0, 0, 15)
            });

            // Create Take-out Order button
            var createTakeoutButton = new Button
            {
                Text = "🛍️ Crear Orden Para Llevar",
                BackgroundColor = Color.FromArgb("#FF9800"),
                TextColor = Colors.White,
                CornerRadius = 8,
                HeightRequest = 50,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Microsoft.Maui.Thickness(0, 5, 0, 5)
            };

            // Available waiters section
            await AddAvailableWaitersSection(popupContent);

            // Create Syncfusion popup
            var popup = new SfPopup
            {
                ContentTemplate = new Microsoft.Maui.Controls.DataTemplate(() => popupContent),
                PopupStyle = new PopupStyle
                {
                    CornerRadius = 12,
                    HasShadow = true,
                    BlurRadius = 3
                },
                StaysOpen = false,
                ShowCloseButton = true,
                ShowFooter = false,
                ShowHeader = false,
                WidthRequest = 450,
                HeightRequest = 350,
                AutoSizeMode = PopupAutoSizeMode.None
            };

            // Wire up the takeout button
            createTakeoutButton.Clicked += (s, e) =>
            {
                popup.Dismiss();
                if (RightPanel.Content is ListTableVisual listTableVisual)
                {
                    // Call the create takeout order method
                    var method = typeof(ListTableVisual).GetMethod("OnCreateTakeoutOrderClicked", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(listTableVisual, new object[] { s, e });
                    }
                }
            };

            // Show the popup
            popup.Show();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al mostrar el popup: {ex.Message}", "OK");
        }
    }

    private async Task AddAvailableWaitersSection(StackLayout parent)
    {
        // Available waiters section header
        parent.Children.Add(new Label
        {
            Text = "� Crear Mesa para Mesero",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#005F8C"),
            Margin = new Microsoft.Maui.Thickness(0, 10, 0, 10)
        });

        // Get available waiters
        var availableWaiters = await AppDbContext.ExecuteSafeAsync(async db =>
            await db.Users
                .Where(u => u.Role == "Mesero" && !u.Deleted && u.Available)
                .ToListAsync());

        if (availableWaiters.Any())
        {
            var waitersGrid = new Grid
            {
                ColumnSpacing = 8,
                RowSpacing = 8,
                HorizontalOptions = LayoutOptions.Fill
            };

            // Set up columns (2 per row for waiters)
            waitersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Microsoft.Maui.GridLength(1, Microsoft.Maui.GridUnitType.Star) });
            waitersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Microsoft.Maui.GridLength(1, Microsoft.Maui.GridUnitType.Star) });

            int row = 0, col = 0;
            foreach (var waiter in availableWaiters)
            {
                // Add row definition if needed
                if (col == 0)
                    waitersGrid.RowDefinitions.Add(new RowDefinition { Height = Microsoft.Maui.GridLength.Auto });

                var waiterButton = new Button
                {
                    Text = $"👤 {waiter.Name}",
                    FontSize = 12,
                    HeightRequest = 40,
                    BackgroundColor = Color.FromArgb("#4CAF50"),
                    TextColor = Colors.White,
                    CornerRadius = 6,
                    HorizontalOptions = LayoutOptions.Fill
                };

                waiterButton.Clicked += (s, e) =>
                {
                    // Find the parent popup and dismiss it
                    var popup = FindParentPopup(waiterButton);
                    popup?.Dismiss();
                    
                    if (RightPanel.Content is ListTableVisual listTableVisual)
                    {
                        // Call the create table method for this waiter
                        var method = typeof(ListTableVisual).GetMethod("OnCreateTableWindowClicked", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        method?.Invoke(listTableVisual, new object[] { waiter });
                    }
                };

                waitersGrid.Children.Add(waiterButton);
                Grid.SetRow(waiterButton, row);
                Grid.SetColumn(waiterButton, col);

                col++;
                if (col >= 2)
                {
                    col = 0;
                    row++;
                }
            }

            parent.Children.Add(waitersGrid);
        }
        else
        {
            parent.Children.Add(new Label
            {
                Text = "No hay meseros disponibles",
                FontAttributes = FontAttributes.Italic,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                FontSize = 12
            });
        }
    }

    private SfPopup? FindParentPopup(Element element)
    {
        var parent = element.Parent;
        while (parent != null)
        {
            if (parent is SfPopup popup)
                return popup;
            parent = parent.Parent;
        }
        return null;
    }



}