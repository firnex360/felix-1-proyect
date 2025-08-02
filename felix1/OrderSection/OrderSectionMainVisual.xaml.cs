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
    private User? _waiter;
    private System.Timers.Timer? _searchBarFocusTimer;

    public OrderSectionMainVisual(CashRegister cashRegister, User waiter)
    {
        InitializeComponent();
        _cashRegister = cashRegister;
        _waiter = waiter;
        if (_waiter != null)
        {
            btnCloseRegister.IsVisible = false;
            RightPanel.Content = new ListWaiterVisual();
        } else {
            DisplayCashRegisterInfo();
            RightPanel.Content = new ListTableVisual();
        }

#if WINDOWS
        var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault();
        if (window != null)
        {
            WindowUtils.MaximizeWindow(window);
        }
#endif

    }

    // Timer for auto-focusing the search bar
    private void StartSearchBarFocusTimer()
    {
        _searchBarFocusTimer = new System.Timers.Timer(5000); // 5 seconds
        _searchBarFocusTimer.Elapsed += (s, e) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Only focus if search bar is not already focused and no popup is open
                if (searchBar != null && !searchBar.IsFocused)
                {
                    searchBar.Focus();
                }
            });
        };
        _searchBarFocusTimer.Start();
    }

    private void StopSearchBarFocusTimer()
    {
        if (_searchBarFocusTimer != null)
        {
            _searchBarFocusTimer.Stop();
            _searchBarFocusTimer.Dispose();
            _searchBarFocusTimer = null;
        }
    }

    private void DisplayCashRegisterInfo()
    {
        lblCashRegisterInfo.Text = $"Caja #{_cashRegister.Number} " /*+
                                 $"Abierta por: {_cashRegister.Cashier?.Name}\n" +
                                 $"Hora de apertura: {_cashRegister.TimeStarted:dd/MM/yyyy HH:mm}\n"*/;
    }

    private async void OnCloseRegister(object sender, EventArgs e)
    {
        if (_cashRegister != null)
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
        if (_waiter != null)
        {
            // Get the ListWaiterVisual instance and pass the search text to it
            if (RightPanel.Content is ListWaiterVisual ListWaiterVisual)
            {
                // Call the filter method to highlight matching table numbers
                ListWaiterVisual.FilterTablesByNumber(searchText);
            }

        }
        else
        {
            // Get the ListTableVisual instance and pass the search text to it
            if (RightPanel.Content is ListTableVisual ListTableVisual)
            {
                // Call the filter method to highlight matching table numbers
                ListTableVisual.FilterTablesByNumber(searchText);
            }
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

        if (_waiter != null)
        {
            // Handle search button press - open the highlighted table
            if (RightPanel.Content is ListWaiterVisual ListWaiterVisual)
            {
                ListWaiterVisual.OpenHighlightedTable();
            }
        } else { 
            // Handle search button press - open the highlighted table
            if (RightPanel.Content is ListTableVisual ListTableVisual)
            {
                ListTableVisual.OpenHighlightedTable();
            }
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
        
        // Start the timer when the page appears
        //StartSearchBarFocusTimer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Stop the timer when the page disappears
        //StopSearchBarFocusTimer();
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
                _ = ShowTakeoutPopup();
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
            else if (RightPanel.Content is ListWaiterVisual ListWaiterVisual)
            {
                ListWaiterVisual.MoveToNextMatchingTable();
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
        } else if (RightPanel.Content is ListWaiterVisual listWaiterVisual)
        {
            // Call the create takeout order method directly
            var method = typeof(ListWaiterVisual).GetMethod("OnCreateTakeoutOrderClicked", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(listWaiterVisual, new object[] { this, EventArgs.Empty });
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
        } else if (RightPanel.Content is ListWaiterVisual ListWaiterVisual)
        {
            ListWaiterVisual.ShowCompletedOrders = e.Value;
            ListWaiterVisual.ReloadTM();
        }
    }

    private async Task ShowQuickAccessPopup()
    {
        try
        {
            // Create the popup content
            var popupContent = new StackLayout
            {
                Spacing = 5,
                Padding = 10,
                BackgroundColor = Colors.White,
                WidthRequest = 400
            };

            // Title
            popupContent.Children.Add(new Label
            {
                Text = "Crear Nueva Mesa",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#005F8C"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Microsoft.Maui.Thickness(0, 0, 0, 0)
            });

            // Add search bar for waiter selection
            var waiterSearchBar = new SearchBar
            {
                Placeholder = "Selecciona un mesero...",
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Microsoft.Maui.Thickness(0, 20, 0, 0),
                BackgroundColor = Color.FromArgb("#f5f7fa"),
                TextColor = Colors.Black
            };


            // Available waiters section
            var availableWaiters = await AddAvailableWaitersSection(popupContent);

            popupContent.Children.Add(waiterSearchBar);

            // Create footer with close button
            var footerContent = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 10,
                Padding = new Microsoft.Maui.Thickness(15, 10)
            };

            var closeButton = new Button
            {
                Text = "Cerrar",
                BackgroundColor = Color.FromArgb("#757575"),
                TextColor = Colors.White,
                CornerRadius = 6,
                WidthRequest = 100,
                HeightRequest = 35,
                FontSize = 14,
                HorizontalOptions = LayoutOptions.End
            };

            footerContent.Children.Add(closeButton);

            // Create Syncfusion popup
            var popup = new SfPopup
            {
                ContentTemplate = new Microsoft.Maui.Controls.DataTemplate(() => popupContent),
                FooterTemplate = new Microsoft.Maui.Controls.DataTemplate(() => footerContent),
                PopupStyle = new PopupStyle
                {
                    CornerRadius = 10,
                    HasShadow = true,
                    BlurRadius = 3,
                    PopupBackground = Colors.White
                },
                StaysOpen = false,
                ShowCloseButton = true,
                ShowFooter = false,
                ShowHeader = false,
                WidthRequest = 450,
                AnimationDuration = 100,
                AutoSizeMode = PopupAutoSizeMode.Height
            };

            // Wire up the close button
            closeButton.Clicked += (s, e) => popup.Dismiss();

            // Wire up the search button pressed event for waiter search bar
            waiterSearchBar.SearchButtonPressed += (s, e) =>
            {
                var searchText = waiterSearchBar.Text?.Trim() ?? "";
                User selectedWaiter = null!;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    // If no search text, select the first waiter
                    selectedWaiter = availableWaiters.FirstOrDefault()!;
                }
                else if (int.TryParse(searchText, out int waiterNumber) && waiterNumber >= 1 && waiterNumber <= availableWaiters.Count)
                {
                    // If it's a valid waiter number, select that waiter by index
                    int index = waiterNumber - 1;
                    selectedWaiter = availableWaiters[index];
                }
                else
                {
                    // Otherwise, search by name
                    selectedWaiter = availableWaiters.FirstOrDefault(w => 
                        w.Name!.ToLower().Contains(searchText.ToLower()))!;
                }

                if (selectedWaiter != null)
                {
                    popup.Dismiss();
                    if (RightPanel.Content is ListTableVisual listTableVisual)
                    {
                        var method = typeof(ListTableVisual).GetMethod("OnCreateTableWindowClicked",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        method?.Invoke(listTableVisual, new object[] { selectedWaiter });
                    } else if (RightPanel.Content is ListWaiterVisual listWaiterVisual)
                    {
                        var method = typeof(ListWaiterVisual).GetMethod("OnCreateTableWindowClicked",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        method?.Invoke(listWaiterVisual, new object[] { selectedWaiter });
                    }
                }
            };

            // Set up key handler for waiter search bar (exact same pattern as OrderVisual)
            waiterSearchBar.HandlerChanged += (s, e) =>
            {
#if WINDOWS
                if (waiterSearchBar?.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.AutoSuggestBox waiterAutoSuggestBox)
                {
                    waiterAutoSuggestBox.KeyDown -= WaiterSearchBarPlatformView_KeyDown;
                    waiterAutoSuggestBox.KeyDown += WaiterSearchBarPlatformView_KeyDown;
                    waiterAutoSuggestBox.KeyUp -= WaiterSearchBarPlatformView_KeyUp;
                    waiterAutoSuggestBox.KeyUp += WaiterSearchBarPlatformView_KeyUp;
                }
#endif
            };

#if WINDOWS
            void WaiterSearchBarPlatformView_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
            {
                if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    popup.Dismiss();
                    e.Handled = true;
                }
            }

            void WaiterSearchBarPlatformView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
            {
                if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    popup.Dismiss();
                    e.Handled = true;
                }
                else if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    // Trigger the SearchButtonPressed event (same as OrderVisual pattern)
                    waiterSearchBar.OnSearchButtonPressed();
                    e.Handled = true;
                }
            }
#endif

            popup.Opened += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100); // make sure it's rendered
                    waiterSearchBar?.Focus();
                });
            };

            // Show the popup
            popup.Show(); 
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al mostrar el popup: {ex.Message}", "OK");
        }
    }

    private async Task<List<User>> AddAvailableWaitersSection(StackLayout parent)
    {
        // Available waiters section header
        parent.Children.Add(new Label
        {
            Text = "",
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
            for (int i = 0; i < availableWaiters.Count; i++)
            {
                var waiter = availableWaiters[i];
                
                // Add row definition if needed
                if (col == 0)
                    waitersGrid.RowDefinitions.Add(new RowDefinition { Height = Microsoft.Maui.GridLength.Auto });

                // Add number indicator for all waiters
                var numberText = $"({i + 1})";
                
                var waiterButton = new Button
                {
                    Text = $"👤 {numberText} {waiter.Name}",
                    FontSize = 12,
                    HeightRequest = 40,
                    BackgroundColor = Color.FromArgb("#005f8c"),
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
                    } else if (RightPanel.Content is ListWaiterVisual listWaiterVisual)
                    {
                        // Call the create table method for this waiter
                        var method = typeof(ListWaiterVisual).GetMethod("OnCreateTableWindowClicked", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        method?.Invoke(listWaiterVisual, new object[] { waiter });
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

        return availableWaiters;
    }

    private async Task<List<Table>> AddActiveTakeoutOrdersSection(StackLayout parent)
    {
        // Available takeout orders section header
        parent.Children.Add(new Label
        {
            Text = "",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#005F8C"),
            Margin = new Microsoft.Maui.Thickness(0, 10, 0, 10)
        });

        // Get active takeout orders for the current cash register
        var activeTakeoutOrders = await AppDbContext.ExecuteSafeAsync(async db =>
        {
            // Get tables that are takeout orders associated with the current cash register
            var takeoutTables = await db.Tables
                .Where(t => t.IsTakeOut && !t.IsPaid)
                .ToListAsync();

            // Filter by cash register through orders if needed
            if (_cashRegister != null)
            {
                var cashRegisterOrders = await db.Orders
                    .Where(o => o.CashRegister != null && o.CashRegister.Id == _cashRegister.Id)
                    .Select(o => o.Table)
                    .Where(t => t != null && t.IsTakeOut && !t.IsPaid)
                    .ToListAsync();
                
                return cashRegisterOrders.Where(t => t != null).ToList()!;
            }
            
            return takeoutTables;
        });

        if (activeTakeoutOrders.Any())
        {
            var takeoutGrid = new Grid
            {
                ColumnSpacing = 8,
                RowSpacing = 8,
                HorizontalOptions = LayoutOptions.Fill
            };

            // Set up columns (2 per row for takeout orders)
            takeoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Microsoft.Maui.GridLength(1, Microsoft.Maui.GridUnitType.Star) });
            takeoutGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new Microsoft.Maui.GridLength(1, Microsoft.Maui.GridUnitType.Star) });

            int row = 0, col = 0;
            for (int i = 0; i < activeTakeoutOrders.Count; i++)
            {
                var takeoutOrder = activeTakeoutOrders[i];
                
                // Add row definition if needed
                if (col == 0)
                    takeoutGrid.RowDefinitions.Add(new RowDefinition { Height = Microsoft.Maui.GridLength.Auto });

                // Add number indicator for all takeout orders
                //var numberText = $"({i + 1})";
                
                var takeoutButton = new Button
                {
                    Text = $"🥡 Para Llevar #{takeoutOrder.LocalNumber}",
                    FontSize = 12,
                    HeightRequest = 40,
                    BackgroundColor = Color.FromArgb("#005f8c"),
                    TextColor = Colors.White,
                    CornerRadius = 6,
                    HorizontalOptions = LayoutOptions.Fill
                };

                takeoutButton.Clicked += (s, e) =>
                {
                    // Find the parent popup and dismiss it
                    var popup = FindParentPopup(takeoutButton);
                    popup?.Dismiss();
                    
                    OpenTakeoutOrder(takeoutOrder);
                };

                takeoutGrid.Children.Add(takeoutButton);
                Grid.SetRow(takeoutButton, row);
                Grid.SetColumn(takeoutButton, col);

                col++;
                if (col >= 2)
                {
                    col = 0;
                    row++;
                }
            }

            parent.Children.Add(takeoutGrid);
        }
        else
        {
            parent.Children.Add(new Label
            {
                Text = "No hay órdenes para llevar activas",
                FontAttributes = FontAttributes.Italic,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                FontSize = 12
            });
        }

        return activeTakeoutOrders;
    }

    private async void OpenTakeoutOrder(Table takeoutTable)
    {
        // First, find the order associated with this table
        var order = await AppDbContext.ExecuteSafeAsync(async db =>
        {
            var openCashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);
            if (openCashRegister == null) return null;

            return await db.Orders
                .Include(o => o.Table)
                .Include(o => o.Waiter)
                .Include(o => o.Items!)
                    .ThenInclude(oi => oi.Article!)
                .FirstOrDefaultAsync(o => o.CashRegister != null &&
                                         o.CashRegister.Id == openCashRegister.Id &&
                                         o.Table != null &&
                                         o.Table.Id == takeoutTable.Id &&
                                         !o.IsDuePaid);
        });

        if (order == null)
        {
            await DisplayAlert("Error", $"No se encontró una orden activa para la mesa para llevar #{takeoutTable.LocalNumber}", "OK");
            return;
        }

        if (RightPanel.Content is ListTableVisual listTableVisual)
        {
            // Call the method to open a specific order (not table)
            var method = typeof(ListTableVisual).GetMethod("OnViewOrderClicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(listTableVisual, new object[] { order });
            }
            else
            {
                await DisplayAlert("Info", $"Abriendo orden para llevar #{takeoutTable.LocalNumber}", "OK");
            }
        }
        else if (RightPanel.Content is ListWaiterVisual listWaiterVisual)
        {
            // Call the method to open a specific order
            var method = typeof(ListWaiterVisual).GetMethod("OnViewOrderClicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(listWaiterVisual, new object[] { order });
            }
            else
            {
                await DisplayAlert("Info", $"Abriendo orden para llevar #{takeoutTable.LocalNumber}", "OK");
            }
        }
    }

    private async Task ShowTakeoutPopup()
    {
        try
        {
            // Create the popup content
            var popupContent = new StackLayout
            {
                Spacing = 5,
                Padding = 10,
                BackgroundColor = Colors.White,
                WidthRequest = 400
            };

            // Title
            popupContent.Children.Add(new Label
            {
                Text = "Ordenes Para Llevar",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#005F8C"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Microsoft.Maui.Thickness(0, 0, 0, 0)
            });

            // Add search bar for takeout order selection
            var takeoutSearchBar = new SearchBar
            {
                Placeholder = "Buscar orden para llevar o escribe número...",
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Microsoft.Maui.Thickness(0, 20, 0, 0),
                BackgroundColor = Color.FromArgb("#f5f7fa"),
                TextColor = Colors.Black
            };

            // Available takeout orders section
            var activeTakeoutOrders = await AddActiveTakeoutOrdersSection(popupContent);

            popupContent.Children.Add(takeoutSearchBar);

            // Create Take-out Order button
            var createTakeoutButton = new Button
            {
                Text = "Crear Nueva Orden (F1)",
                BackgroundColor = Color.FromArgb("#005f8c"),
                TextColor = Colors.White,
                CornerRadius = 6,
                HeightRequest = 40,
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Fill,
                Margin = new Microsoft.Maui.Thickness(0, 10, 0, 0)
            };

            popupContent.Children.Add(createTakeoutButton);

            // Create footer with close button
            var footerContent = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                HorizontalOptions = LayoutOptions.Center,
                Spacing = 10,
                Padding = new Microsoft.Maui.Thickness(15, 10)
            };

            var closeButton = new Button
            {
                Text = "Cerrar",
                BackgroundColor = Color.FromArgb("#757575"),
                TextColor = Colors.White,
                CornerRadius = 6,
                WidthRequest = 100,
                HeightRequest = 35,
                FontSize = 14,
                HorizontalOptions = LayoutOptions.End
            };

            footerContent.Children.Add(closeButton);

            // Create Syncfusion popup
            var popup = new SfPopup
            {
                ContentTemplate = new Microsoft.Maui.Controls.DataTemplate(() => popupContent),
                FooterTemplate = new Microsoft.Maui.Controls.DataTemplate(() => footerContent),
                PopupStyle = new PopupStyle
                {
                    CornerRadius = 10,
                    HasShadow = true,
                    BlurRadius = 3,
                    PopupBackground = Colors.White
                },
                StaysOpen = false,
                ShowCloseButton = true,
                ShowFooter = false,
                ShowHeader = false,
                WidthRequest = 450,
                AnimationDuration = 100,
                AutoSizeMode = PopupAutoSizeMode.Height
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
                        method.Invoke(listTableVisual, new object[] { s!, e });
                    }
                } else if (RightPanel.Content is ListWaiterVisual listWaiterVisual)
                {
                    // Call the create takeout order method
                    var method = typeof(ListWaiterVisual).GetMethod("OnCreateTakeoutOrderClicked", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(listWaiterVisual, new object[] { s!, e });
                    }
                }
            };

            // Wire up the close button
            closeButton.Clicked += (s, e) => popup.Dismiss();

            // Wire up the search button pressed event for takeout order search bar
            takeoutSearchBar.SearchButtonPressed += (s, e) =>
            {
                var searchText = takeoutSearchBar.Text?.Trim() ?? "";
                Table selectedTakeoutOrder = null!;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    // If no search text, select the first takeout order
                    selectedTakeoutOrder = activeTakeoutOrders.FirstOrDefault()!;
                }
                else if (int.TryParse(searchText, out int orderNumber) && orderNumber >= 1 && orderNumber <= activeTakeoutOrders.Count)
                {
                    // If it's a valid order number, select that order by index
                    int index = orderNumber - 1;
                    selectedTakeoutOrder = activeTakeoutOrders[index];
                }
                else
                {
                    // Otherwise, search by table number
                    selectedTakeoutOrder = activeTakeoutOrders.FirstOrDefault(t => 
                        t.LocalNumber.ToString().Contains(searchText) || 
                        t.GlobalNumber.ToString().Contains(searchText))!;
                }

                if (selectedTakeoutOrder != null)
                {
                    popup.Dismiss();
                    OpenTakeoutOrder(selectedTakeoutOrder);
                }
            };

            // Set up key handler for takeout search bar (exact same pattern as OrderVisual)
            takeoutSearchBar.HandlerChanged += (s, e) =>
            {
#if WINDOWS
                if (takeoutSearchBar?.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.AutoSuggestBox takeoutAutoSuggestBox)
                {
                    takeoutAutoSuggestBox.KeyDown -= TakeoutSearchBarPlatformView_KeyDown;
                    takeoutAutoSuggestBox.KeyDown += TakeoutSearchBarPlatformView_KeyDown;
                    takeoutAutoSuggestBox.KeyUp -= TakeoutSearchBarPlatformView_KeyUp;
                    takeoutAutoSuggestBox.KeyUp += TakeoutSearchBarPlatformView_KeyUp;
                }
#endif
            };

#if WINDOWS
            void TakeoutSearchBarPlatformView_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
            {
                if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    popup.Dismiss();
                    e.Handled = true;
                }
            }

            void TakeoutSearchBarPlatformView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
            {
                if (e.Key == Windows.System.VirtualKey.F1)
                {
                    // F1 to create new takeout order
                    popup.Dismiss();
                    if (RightPanel.Content is ListTableVisual listTableVisual)
                    {
                        var method = typeof(ListTableVisual).GetMethod("OnCreateTakeoutOrderClicked", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (method != null)
                        {
                            method.Invoke(listTableVisual, new object[] { sender, EventArgs.Empty });
                        }
                    } else if (RightPanel.Content is ListWaiterVisual listWaiterVisual)
                    {
                        var method = typeof(ListWaiterVisual).GetMethod("OnCreateTakeoutOrderClicked", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (method != null)
                        {
                            method.Invoke(listWaiterVisual, new object[] { sender, EventArgs.Empty });
                        }
                    }
                    e.Handled = true;
                }
                else if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    popup.Dismiss();
                    e.Handled = true;
                }
                else if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    // Only proceed if there's content in the search bar
                    if (!string.IsNullOrWhiteSpace(takeoutSearchBar.Text))
                    {
                        // Trigger the SearchButtonPressed event (same as OrderVisual pattern)
                        takeoutSearchBar.OnSearchButtonPressed();
                    }
                    e.Handled = true;
                }
            }
#endif

            popup.Opened += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100); // make sure it's rendered
                    takeoutSearchBar?.Focus();
                });
            };

            // Show the popup
            popup.Show(); 
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al mostrar el popup: {ex.Message}", "OK");
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