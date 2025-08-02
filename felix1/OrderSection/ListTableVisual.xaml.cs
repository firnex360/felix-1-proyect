using System.Collections.ObjectModel;
using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;

namespace felix1.OrderSection;

public partial class ListTableVisual : ContentView
{
    public static ListTableVisual? Instance { get; private set; }
    public ObservableCollection<Table> Tables { get; set; } = new();


    // Add tracking for currently highlighted table
    private Table? _currentHighlightedTable = null;
    private bool _isGlobalSearchMatch = false;
    public bool ShowCompletedOrders { get; set; } = false;

    // Add tracking for multiple matching tables (Tab navigation)
    private List<Frame> _matchingLocalFrames = new();
    private List<Table> _matchingLocalTables = new();
    private int _activeLocalFrameIndex = 0;

    public ListTableVisual()
    {
        InitializeComponent();
        BindingContext = this;
        Instance = this;
        LoadMeseros();
        LoadExistingTakeoutOrders();
    }

    private Color GetOrderButtonColor(Order order)
    {
        if (ShowCompletedOrders)
        {
            return Color.FromArgb("#FD2D2D"); //CUANDO QUIERES REALIZAR REFUND
        }
        else
        {
            if (order.IsBillRequested)
            {
                return Color.FromArgb("#4CAF50"); // CUANDO YA IMPRIMES  
            }
            else
            {
                return Color.FromArgb("#005F8C"); // CUANDO ACABAS DE GENERAR LA ORDEN
            }
        }
    }

    private async void LoadMeseros()
    {
        var meseros = await AppDbContext.ExecuteSafeAsync(async db =>
            await db.Users
                .Where(u => u.Role == "Mesero" && !u.Deleted && u.Available)
                .ToListAsync());

        var tableOrders = await AppDbContext.ExecuteSafeAsync(async db =>
        {
            var openCashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);
            if (openCashRegister == null) return new List<Order>();

            // Load orders with all required relationships
            var orders = await db.Orders
                .Include(o => o.Table)
                .Include(o => o.Waiter)
                .Include(o => o.Items!)
                    .ThenInclude(oi => oi.Article)
                .Where(o => o.Table != null &&
                           o.Waiter != null &&
                           o.CashRegister == openCashRegister)
                .ToListAsync();

            // Filtrar órdenes basado en el estado de pago
            var filteredOrders = new List<Order>();
            foreach (var order in orders)
            {
                bool hasRefund = await HasRefund(order);

                if (ShowCompletedOrders)
                {
                    // Mostrar solo órdenes pagadas sin reembolsos
                    if (order.Table?.IsPaid == true && !hasRefund)
                    {
                        filteredOrders.Add(order);
                    }
                }
                else
                {
                    // Mostrar solo órdenes no pagadas
                    if (order.Table?.IsPaid == false)
                    {
                        filteredOrders.Add(order);
                    }
                }
            }

            return filteredOrders;
        });

        MeseroContainer.Children.Clear();

        foreach (var user in meseros)
        {
            var userTables = tableOrders
                .Where(o => o.Waiter?.Id == user.Id && o.Table != null)
                .Select(o => o.Table!)
                .Distinct()
                .ToList();

            var stack = new VerticalStackLayout
            {
                Spacing = 12,
                VerticalOptions = LayoutOptions.Start,
            };

            // Mesero's name
            stack.Children.Add(new Label
            {
                Text = user.Name,
                FontSize = 18,
                TextColor = Colors.Black,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center
            });

            //ADD AVAILABLE TABLES WITHIN THE ORDERS THAT THE MESERO HAS
            // Create a horizontal layout to hold all table frames
            var tableRow = new VerticalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Fill,
            };

            foreach (var table in userTables)
            {
                var ordersForTable = tableOrders
                    .Where(o => o.Table?.Id == table.Id)
                    .OrderBy(o => o.OrderNumber)
                    .ToList();

                var tableContent = new VerticalStackLayout
                {
                    Spacing = 5,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Start
                };

                // Table number label
                tableContent.Children.Add(new Label
                {
                    Text = $"Mesa #{table.LocalNumber} - #{table.GlobalNumber}",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.Black
                });

                // Add buttons for each order
                foreach (var order in ordersForTable)
                {
                    var orderButton = new Button
                    {
                        Text = $"Orden #{order.OrderNumber} || Total: {findOrderTotal(order):C}",
                        FontSize = 12,
                        HeightRequest = 30,
                        BackgroundColor = GetOrderButtonColor(order),
                        TextColor = Colors.White,
                        CornerRadius = 5,
                        HorizontalOptions = LayoutOptions.Fill,
                        Command = new Command(() => {
                            if (ShowCompletedOrders)
                                RefundVisual(order);
                            else
                                OnViewOrderClicked(order);
                        })
                    };
                    tableContent.Children.Add(orderButton);
                }

                // Determine frame border color based on orders
                Color frameBorderColor = ordersForTable.Any() ?
                GetOrderButtonColor(ordersForTable.First()) :
                Color.FromArgb("#C7CFDD");

                var tableFrame = new Frame
                {
                    HeightRequest = 100,
                    BackgroundColor = Colors.White,
                    CornerRadius = 10,
                    Padding = 8,
                    Content = tableContent,
                    BorderColor = frameBorderColor,
                    HorizontalOptions = LayoutOptions.Fill
                };

                tableRow.Children.Add(tableFrame);
            }

            stack.Children.Add(tableRow);

            // Add "Create Table" button
            stack.Children.Add(new Button
            {
                Text = "Crear Mesa",
                BackgroundColor = Color.FromArgb("#005F8C"),
                TextColor = Colors.White,
                CornerRadius = 8,
                HeightRequest = 40,
                WidthRequest = 120,
                HorizontalOptions = LayoutOptions.Center,
                Command = new Command(() => OnCreateTableWindowClicked(user))
            });

            var card = new Frame
            {
                WidthRequest = 300,
                CornerRadius = 10,
                Padding = 10,
                BackgroundColor = Colors.White,
                BorderColor = Colors.White,
                Content = stack,
            };

            MeseroContainer.Children.Add(card);
        }
    }

    private decimal findOrderTotal(Order order)
    {
        var subtotal = order.Items!
            .GroupBy(item => item.Id) // Group by unique ID
            .Select(group => group.First()) // Take first instance of each
            .Sum(item => item.Quantity * item.UnitPrice); // Sum distinct items

        var taxRateString = Preferences.Get("TaxRate", "0");
        var waiterTaxRateString = Preferences.Get("WaiterTaxRate", "0");
        
        // Safely parse tax rates with fallback to 0
        var taxRate = (decimal.TryParse(taxRateString, out var parsedTaxRate) ? parsedTaxRate : 0m) / 100m * subtotal;
        var waiterTaxRate = (decimal.TryParse(waiterTaxRateString, out var parsedWaiterTaxRate) ? parsedWaiterTaxRate : 0m) / 100m * subtotal;

        return subtotal + waiterTaxRate + taxRate - order.Discount;
    }

    private async void RefundVisual(Order order)
    {
        try
        {
            // Verificar si tiene al menos una transacción
            bool hasTransaction = await AppDbContext.ExecuteSafeAsync(async db =>
                await db.Transactions.AnyAsync(t => t.Order != null && t.Order.Id == order.Id));

            if (!hasTransaction)
            {
                await Application.Current!.MainPage!.DisplayAlert("Error",
                    "No se puede reembolsar una orden que no tiene transacciones registradas.", "OK");
                return;
            }

            var loadedOrder = await AppDbContext.ExecuteSafeAsync(async db =>
            {
                return await db.Orders
                    .AsNoTracking()
                    .Include(o => o.Table)
                    .Include(o => o.Waiter)
                    .Include(o => o.CashRegister)
                    .Include(o => o.Items!)
                        .ThenInclude(oi => oi.Article)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);
            });

            if (loadedOrder == null)
            {
                await Application.Current!.MainPage!.DisplayAlert("Error", "No se pudo cargar la orden.", "OK");
                return;
            }

            // Verificar que los Items no sean null
            loadedOrder.Items ??= new List<OrderItem>();

            var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
            ContentPage targetPage = new RefundVisual(loadedOrder);

            var window = new Window(targetPage)
            {
                Height = 700,
                Width = 1000,
                X = (displayInfo.Width / displayInfo.Density - 1000) / 2,
                Y = ((displayInfo.Height / displayInfo.Density - 700) / 2) - 25
            };

            Application.Current?.OpenWindow(window);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en RefundVisual: {ex.Message}");
            await Application.Current!.MainPage!.DisplayAlert("Error", "Ocurrió un error al cargar la orden.", "OK");
        }
    }

    private async void OnCreateTableWindowClicked(User user)
    {
        await AppDbContext.ExecuteSafeAsync(async db =>
        {
            var waiter = await AppDbContext.ExecuteSafeAsync(async db =>
                await db.Users.FindAsync(user.Id));

            if (waiter == null)
            {
                if (Application.Current?.MainPage != null)
                    await Application.Current.MainPage.DisplayAlert("Error", "Mesero no encontrado.", "OK");
                return;
            }

            Table? table = null;
            int orderNumber = 0;
            await AppDbContext.ExecuteSafeAsync(async db =>
            {
                var cashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);
                if (cashRegister == null)
                {
                    if (Application.Current?.MainPage != null)
                        await Application.Current.MainPage.DisplayAlert("Error", "No hay una caja abierta.", "OK");
                    return;
                }

                orderNumber = await db.Orders
                    .Where(o => o.CashRegister != null && o.CashRegister.Id == cashRegister.Id)
                    .CountAsync();

                // ALL TABLES IN THE **OPEN** CASH REGISTER
                var allTables = await db.Orders
                    .Include(o => o.Table)
                    .Include(o => o.Waiter)
                    .Where(o => o.CashRegister != null && o.CashRegister.Id == cashRegister.Id && o.Table != null)
                    .Select(o => o.Table!)
                    .ToListAsync();

                // ALL TABLES FROM CURRENT WAITER
                var waiterTables = await db.Orders
                    .Include(o => o.Table)
                    .Where(o => o.CashRegister != null &&
                                o.CashRegister.Id == cashRegister.Id &&
                                o.Waiter != null &&
                                o.Waiter.Id == waiter!.Id &&
                                o.Table != null)
                    .Select(o => o.Table!)
                    .ToListAsync();

                table = new Table
                {
                    LocalNumber = waiterTables.Count + 1,
                    GlobalNumber = allTables.Count + 101, //global numbers start from 100
                    IsTakeOut = false,
                    IsBillRequested = false,
                    IsPaid = false
                };

                db.Tables.Add(table);
                await db.SaveChangesAsync();
            });

            await AppDbContext.ExecuteSafeAsync(async db =>
            {
                if (table == null) return;

                var savedTable = await db.Tables.FirstOrDefaultAsync(t => t.Id == table!.Id);
                var cashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);

                if (savedTable == null || cashRegister == null)
                {
                    if (Application.Current?.MainPage != null)
                        await Application.Current.MainPage.DisplayAlert("Error", "Datos no válidos al crear la orden.", "OK");
                    return;
                }

                if (waiter != null)
                    db.Attach(waiter);
                db.Attach(cashRegister);

                var newOrder = new Order
                {
                    OrderNumber = orderNumber + 1,
                    Date = DateTime.Now,
                    Waiter = waiter,
                    Table = savedTable,
                    Items = null,
                    CashRegister = cashRegister,
                    IsDuePaid = false,
                    IsBillRequested = false
                };

                db.Orders.Add(newOrder);
                await db.SaveChangesAsync();
                OnViewOrderClicked(newOrder);
            });
            ReloadTM();
        });
    }

    private void AddTakeoutOrderToPanel(Order order)
    {
        int displayOrderNumber = order.Table?.LocalNumber ?? order.OrderNumber;

        var orderButton = new Button
        {
            Text = $"Orden #{displayOrderNumber}",
            FontSize = 12,
            HeightRequest = 30,
            //WidthRequest = 90,
            BackgroundColor = GetOrderButtonColor(order),
            TextColor = Colors.White,
            CornerRadius = 5,
            HorizontalOptions = LayoutOptions.Fill,
            Command = new Command(() => {
                if (order.IsDuePaid)
                    RefundVisual(order);
                else
                    OnViewOrderClicked(order);
            })
        };

        // Find the container in the visual tree
        if (this.FindByName("TakeoutOrdersContainer") is VerticalStackLayout container)
        {
            container.Children.Add(orderButton);
        }
    }

    private async void LoadExistingTakeoutOrders()
    {
        if (this.FindByName("TakeoutOrdersContainer") is VerticalStackLayout container)
        {
            container.Children.Clear();

            var takeoutOrders = await AppDbContext.ExecuteSafeAsync(async db =>
            {
                var openCashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);
                if (openCashRegister == null) return new List<Order>();

                // Cargar órdenes para llevar
                var orders = await db.Orders
                    .Include(o => o.Table)
                    .Where(o => o.Table != null &&
                               o.Table.IsTakeOut &&
                               o.CashRegister == openCashRegister)
                    .OrderBy(o => o.OrderNumber)
                    .ToListAsync();

                // Filtrar basado en estado de pago
                var filteredOrders = new List<Order>();
                foreach (var order in orders)
                {
                    bool hasRefund = await HasRefund(order);

                    if (ShowCompletedOrders)
                    {
                        // Mostrar solo órdenes pagadas sin reembolsos
                        if (order.Table?.IsPaid == true && !hasRefund)
                        {
                            filteredOrders.Add(order);
                        }
                    }
                    else
                    {
                        // Mostrar solo órdenes no pagadas
                        if (order.Table?.IsPaid == false)
                        {
                            filteredOrders.Add(order);
                        }
                    }
                }

                return filteredOrders;
            });

            foreach (var order in takeoutOrders)
            {
                AddTakeoutOrderToPanel(order);
            }

            var createButton = new Button
            {
                Text = "Crear Pedido",
                BackgroundColor = Color.FromArgb("#005F8C"),
                TextColor = Colors.White,
                CornerRadius = 8,
                HeightRequest = 40,
                WidthRequest = 120,
                HorizontalOptions = LayoutOptions.Center,
                Command = new Command(() => OnCreateTakeoutOrderClicked(this, EventArgs.Empty))
            };
            container.Children.Add(createButton);
        }
    }

    private async void OnCreateTakeoutOrderClicked(object sender, EventArgs e)
    {
        await AppDbContext.ExecuteSafeAsync(async db =>
        {
            var waiter = await db.Users.FirstOrDefaultAsync(u => u.Name == "TAKEOUT");
            var cashRegister = await db.CashRegisters
                .Include(c => c.Cashier)  // Include the Cashier relationship
                .FirstOrDefaultAsync(c => c.IsOpen);

            if (cashRegister == null)
            {
                await Application.Current!.MainPage!.DisplayAlert("Error", "No hay una caja abierta.", "OK");
                return;
            }

            // Debug: Check if cashier is loaded
            Console.WriteLine($"Cash Register Cashier: {cashRegister.Cashier?.Name ?? "NULL"}");

            var existingTakeouts = await db.Orders
                .Where(o => o.CashRegister!.Id == cashRegister.Id && o.Table != null && o.Table.IsTakeOut)
                .Select(o => o.Table!)
                .ToListAsync();

            var allTables = await db.Orders
                .Include(o => o.Table)
                .Include(o => o.Waiter)
                .Where(o => o.CashRegister != null && o.CashRegister.Id == cashRegister.Id && o.Table != null)
                .Select(o => o.Table!)
                .ToListAsync();

            int nextTakeoutNumber = (existingTakeouts.Count + 1);

            var table = new Table
            {
                LocalNumber = nextTakeoutNumber,
                GlobalNumber = allTables.Count + 101, // global numbers start from 100
                IsTakeOut = true,
                IsBillRequested = false,
                IsPaid = false
            };

            db.Tables.Add(table);
            await db.SaveChangesAsync();

            int orderNumber = await db.Orders
                .Where(o => o.CashRegister!.Id == cashRegister.Id)
                .CountAsync();

            db.Attach(cashRegister);
            if (waiter != null) db.Attach(waiter);

            var order = new Order
            {
                OrderNumber = orderNumber + 1,
                Date = DateTime.Now,
                Waiter = waiter,
                Table = table,
                Items = null,
                CashRegister = cashRegister,
                IsDuePaid = false,
                IsBillRequested = false
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            LoadExistingTakeoutOrders();
            OnViewOrderClicked(order);
        });
    }

    private async void OnViewOrderClicked(Order order)
    {
        try
        {
            var loadedOrder = await AppDbContext.ExecuteSafeAsync(async db =>
                await db.Orders
                    .Include(o => o.Table)
                    .Include(o => o.Waiter)
                    .Include(o => o.Items!)
                    .ThenInclude(oi => oi.Article!)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == order.Id));

            if (loadedOrder == null)
            {
                if (Application.Current?.MainPage != null)
                    await Application.Current.MainPage.DisplayAlert("Error", "No se pudo cargar la orden.", "OK");
                return;
            }

            var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
            ContentPage targetPage = loadedOrder.IsBillRequested
                ? new PaymentVisual(loadedOrder)
                : new OrderVisual(loadedOrder);

            int height = 800;
            int width = 1000;
            var window = new Window(targetPage)
            {
                Height = height,
                Width = width,
                X = (displayInfo.Width / displayInfo.Density - width) / 2,
                Y = ((displayInfo.Height / displayInfo.Density - height) / 2)
            };

            Application.Current?.OpenWindow(window);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    private void LoadTables()
    {
        var tablesFromDb = AppDbContext.ExecuteSafeAsync(async db =>
            await db.Tables.ToListAsync())
            .GetAwaiter().GetResult();

        Tables.Clear();
        foreach (var table in tablesFromDb)
            Tables.Add(table);
    }

    public void ReloadTM()
    {
        LoadTables();
        LoadMeseros();
        LoadExistingTakeoutOrders();
    }

    // Method to open the currently highlighted table's order
    public async void OpenHighlightedTable()
    {
        if (_currentHighlightedTable == null) return;

        // Find the order for the highlighted table
        var order = await AppDbContext.ExecuteSafeAsync(async db =>
        {
            var openCashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);
            if (openCashRegister == null) return null;

            return await db.Orders
                .FirstOrDefaultAsync(o => o.CashRegister != null &&
                                         o.CashRegister.Id == openCashRegister.Id &&
                                         o.Table != null &&
                                         o.Table.Id == _currentHighlightedTable.Id &&
                                         !o.IsDuePaid);
        });

        if (order != null)
        {
            // Use the existing OnViewOrderClicked method
            OnViewOrderClicked(order);
        }
        else
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert("Info",
                    $"No se encontró una orden activa para la Mesa #{_currentHighlightedTable.LocalNumber}", "OK");
            }
        }
    }

    //for search by table number
    public void FilterTablesByNumber(string searchText)
    {
        // Reset all frames to default appearance first
        ResetAllTableFrames();

        // Clear tracking collections
        _matchingLocalFrames.Clear();
        _matchingLocalTables.Clear();
        _activeLocalFrameIndex = 0;

        // Clear highlighted table when search is cleared
        if (string.IsNullOrWhiteSpace(searchText))
        {
            _currentHighlightedTable = null;
            _isGlobalSearchMatch = false;
            return;
        }

        // Try to parse the search text as a number
        if (int.TryParse(searchText.Trim(), out int searchNumber))
        {
            // Priority-based search: Global first, then Local
            bool foundMatch = false;

            // First try to find by Global Number
            foundMatch = HighlightTableByGlobalNumber(searchNumber);

            // If no global match found, try Local Number and collect all matches
            if (!foundMatch)
            {
                CollectAndHighlightLocalMatches(searchNumber);
            }
        }
        else
        {
            _currentHighlightedTable = null;
            _isGlobalSearchMatch = false;
        }
    }

    private void ResetAllTableFrames()
    {
        foreach (var meseroCard in MeseroContainer.Children.OfType<Frame>())
        {
            if (meseroCard.Content is VerticalStackLayout meseroStack)
            {
                foreach (var child in meseroStack.Children)
                {
                    if (child is VerticalStackLayout tableRow)
                    {
                        foreach (var tableFrame in tableRow.Children.OfType<Frame>())
                        {
                            tableFrame.BorderColor = Color.FromArgb("#C7CFDD");
                            tableFrame.BackgroundColor = Colors.White;
                            tableFrame.HasShadow = false;
                            tableFrame.Scale = 1.0; // Reset any scaling from animations
                            tableFrame.CornerRadius = 10; // Preserve the corner radius
                        }
                    }
                }
            }
        }
    }

    private bool HighlightTableByGlobalNumber(int globalNumber)
    {
        bool foundMatch = false;
        _currentHighlightedTable = null;
        _isGlobalSearchMatch = false;

        // First, we need to get all tables with their global numbers from the database
        var allTablesWithGlobal = AppDbContext.ExecuteSafeAsync(async db =>
        {
            var openCashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);
            if (openCashRegister == null) return new List<Table>();

            return await db.Orders
                .Include(o => o.Table)
                .Include(o => o.Waiter)
                .Where(o => o.CashRegister != null &&
                           o.CashRegister.Id == openCashRegister.Id &&
                           o.Table != null &&
                           !o.IsDuePaid)
                .Select(o => o.Table!)
                .Distinct()
                .ToListAsync();
        }).GetAwaiter().GetResult();

        // Find the table with matching global number
        var targetTable = allTablesWithGlobal.FirstOrDefault(t => t.GlobalNumber == globalNumber);

        if (targetTable != null)
        {
            _currentHighlightedTable = targetTable;
            _isGlobalSearchMatch = true;
            // Find and highlight the frame for this table
            foundMatch = HighlightTableFrame(targetTable, true); // true indicates global search
        }

        return foundMatch;
    }

    private bool HighlightTableFrame(Table targetTable, bool isGlobalSearch)
    {
        bool foundMatch = false;

        foreach (var meseroCard in MeseroContainer.Children.OfType<Frame>())
        {
            if (meseroCard.Content is VerticalStackLayout meseroStack)
            {
                foreach (var child in meseroStack.Children)
                {
                    if (child is VerticalStackLayout tableRow)
                    {
                        foreach (var tableFrame in tableRow.Children.OfType<Frame>())
                        {
                            if (tableFrame.Content is VerticalStackLayout tableContent)
                            {
                                // Find the table number label and check if it matches our target
                                var tableLabel = tableContent.Children.OfType<Label>()
                                    .FirstOrDefault(l => l.Text != null && l.Text.StartsWith("Mesa #"));

                                if (tableLabel != null)
                                {
                                    var labelText = tableLabel.Text.Replace("Mesa #", "").Split(" - ")[0];
                                    if (int.TryParse(labelText, out int displayedLocalNumber))
                                    {
                                        // For global search, we need to check if this displayed table
                                        // corresponds to our target table by comparing the actual table data
                                        // that's being displayed in this UI frame

                                        // Get the table data that corresponds to this UI frame
                                        // We need to find which table from our loaded data matches this display
                                        if (isGlobalSearch)
                                        {
                                            // For global search, we're looking for the table with the specific GlobalNumber
                                            // and LocalNumber combination that matches our target
                                            if (displayedLocalNumber == targetTable.LocalNumber)
                                            {
                                                // We found a potential match, but we need to verify it's the right table
                                                // by checking if this waiter section contains our target table
                                                var meseroNameLabel = meseroStack.Children.OfType<Label>()
                                                    .FirstOrDefault(l => l.FontAttributes == FontAttributes.Bold);

                                                if (meseroNameLabel != null)
                                                {
                                                    // Get the waiter's tables to verify this is the correct table
                                                    var waiterName = meseroNameLabel.Text;

                                                    // Check if our target table belongs to this waiter by verifying
                                                    // that this waiter has a table with this LocalNumber AND GlobalNumber
                                                    if (DoesWaiterHaveTable(waiterName, targetTable))
                                                    {
                                                        ApplyHighlightStyling(tableFrame, isGlobalSearch);
                                                        foundMatch = true;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // For local search, just match by local number as before
                                            if (displayedLocalNumber == targetTable.LocalNumber)
                                            {
                                                ApplyHighlightStyling(tableFrame, isGlobalSearch);
                                                foundMatch = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (foundMatch) break;
                    }
                }
                if (foundMatch) break;
            }
        }

        return foundMatch;
    }

    private bool DoesWaiterHaveTable(string waiterName, Table targetTable)
    {
        // Get the waiter's tables from database to verify
        var waiterTables = AppDbContext.ExecuteSafeAsync(async db =>
        {
            var openCashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);
            if (openCashRegister == null) return new List<Table>();

            var waiter = await db.Users.FirstOrDefaultAsync(u => u.Name == waiterName && u.Role == "Mesero");
            if (waiter == null) return new List<Table>();

            return await db.Orders
                .Include(o => o.Table)
                .Include(o => o.Waiter)
                .Where(o => o.CashRegister != null &&
                           o.CashRegister.Id == openCashRegister.Id &&
                           o.Table != null &&
                           o.Waiter != null &&
                           o.Waiter.Id == waiter.Id &&
                           !o.IsDuePaid)
                .Select(o => o.Table!)
                .Distinct()
                .ToListAsync();
        }).GetAwaiter().GetResult();

        // Check if any of this waiter's tables matches our target table
        return waiterTables.Any(t => t.Id == targetTable.Id ||
                                   (t.LocalNumber == targetTable.LocalNumber &&
                                    t.GlobalNumber == targetTable.GlobalNumber));
    }

    private void ApplyHighlightStyling(Frame tableFrame, bool isGlobalSearch)
    {
        if (isGlobalSearch)
        {
            // Global search styling - more prominent
            tableFrame.BorderColor = Color.FromArgb("#FF5722"); // Orange-Red for global
            tableFrame.BackgroundColor = Color.FromArgb("#FFF3E0"); // Light orange background
            tableFrame.HasShadow = true;
            tableFrame.CornerRadius = 10;
            tableFrame.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("#FF5722")),
                Offset = new Point(2, 2),
                Radius = 5,
                Opacity = 0.7f
            };
            // Add a small animation effect
            _ = AnimateHighlight(tableFrame);
        }
        else
        {
            // highlight for non-active frames when cycling with Tab
            tableFrame.BorderColor = Color.FromArgb("#2196F3"); // Blue for local search
            tableFrame.BackgroundColor = Color.FromArgb("#E3F2FD"); // Light blue background
            tableFrame.HasShadow = true;
            tableFrame.CornerRadius = 10;
            tableFrame.Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Color.FromArgb("#2196F3")),
                Offset = new Point(2, 2),
                Radius = 5,
                Opacity = 0.7f
            };
        }
    }

    private async Task AnimateHighlight(Frame frame)
    {
        // Simple scale animation for global search results
        await frame.ScaleTo(1.05, 150, Easing.BounceOut);
        await frame.ScaleTo(1.0, 150, Easing.BounceOut);
    }

    private void CollectAndHighlightLocalMatches(int localNumber)
    {
        // Get all tables to find those with matching local number
        var allTables = AppDbContext.ExecuteSafeAsync(async db =>
        {
            var openCashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);
            if (openCashRegister == null) return new List<Table>();

            return await db.Orders
                .Include(o => o.Table)
                .Include(o => o.Waiter)
                .Where(o => o.CashRegister != null &&
                           o.CashRegister.Id == openCashRegister.Id &&
                           o.Table != null &&
                           !o.IsDuePaid)
                .Select(o => o.Table!)
                .Distinct()
                .ToListAsync();
        }).GetAwaiter().GetResult();

        // Find all matching tables and frames
        var matchingTables = allTables.Where(t => t.LocalNumber == localNumber).ToList();

        // Search through all visible table frames and collect matching ones
        foreach (var meseroCard in MeseroContainer.Children.OfType<Frame>())
        {
            if (meseroCard.Content is VerticalStackLayout meseroStack)
            {
                foreach (var child in meseroStack.Children)
                {
                    if (child is VerticalStackLayout tableRow)
                    {
                        foreach (var tableFrame in tableRow.Children.OfType<Frame>())
                        {
                            if (tableFrame.Content is VerticalStackLayout tableContent)
                            {
                                var tableLabel = tableContent.Children.OfType<Label>()
                                    .FirstOrDefault(l => l.Text != null && l.Text.StartsWith("Mesa #"));

                                if (tableLabel != null)
                                {
                                    var labelText = tableLabel.Text.Replace("Mesa #", "").Split(" - ")[0];
                                    
                                    if (int.TryParse(labelText, out int tableLocalNumber) && tableLocalNumber == localNumber)
                                    {
                                        // Find the corresponding table from database
                                        var matchingTable = matchingTables.FirstOrDefault(t =>
                                            DoesFrameMatchTable(tableFrame, t));

                                        if (matchingTable != null)
                                        {
                                            _matchingLocalFrames.Add(tableFrame);
                                            _matchingLocalTables.Add(matchingTable);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Highlight all matching frames, with special highlight for the active one
        for (int i = 0; i < _matchingLocalFrames.Count; i++)
        {
            if (i == _activeLocalFrameIndex)
            {
                ApplyActiveTabHighlight(_matchingLocalFrames[i]);
            }
            else
            {
                ApplyInactiveTabHighlight(_matchingLocalFrames[i]);
            }
        }

        // Set the current highlighted table to the active one
        if (_matchingLocalTables.Count > 0)
        {
            _currentHighlightedTable = _matchingLocalTables[_activeLocalFrameIndex];
        }
    }

    private bool DoesFrameMatchTable(Frame frame, Table table)
    {
        // Get the waiter name from the frame's parent structure
        var meseroCard = GetParentMeseroCard(frame);
        if (meseroCard?.Content is VerticalStackLayout meseroStack)
        {
            var waiterLabel = meseroStack.Children.OfType<Label>().FirstOrDefault();
            if (waiterLabel != null)
            {
                return DoesWaiterHaveTable(waiterLabel.Text, table);
            }
        }
        return false;
    }

    private Frame? GetParentMeseroCard(Frame tableFrame)
    {
        // Navigate up the visual tree to find the mesero card
        foreach (var meseroCard in MeseroContainer.Children.OfType<Frame>())
        {
            if (meseroCard.Content is VerticalStackLayout meseroStack)
            {
                foreach (var child in meseroStack.Children)
                {
                    if (child is VerticalStackLayout tableRow)
                    {
                        if (tableRow.Children.Contains(tableFrame))
                        {
                            return meseroCard;
                        }
                    }
                }
            }
        }
        return null;
    }

    public void MoveToNextMatchingTable()
    {
        if (_matchingLocalFrames.Count > 1)
        {
            _activeLocalFrameIndex = (_activeLocalFrameIndex + 1) % _matchingLocalFrames.Count;

            // Re-highlight all frames
            for (int i = 0; i < _matchingLocalFrames.Count; i++)
            {
                if (i == _activeLocalFrameIndex)
                {
                    ApplyActiveTabHighlight(_matchingLocalFrames[i]);
                }
                else
                {
                    ApplyInactiveTabHighlight(_matchingLocalFrames[i]);
                }
            }

            // Update the current highlighted table
            if (_activeLocalFrameIndex < _matchingLocalTables.Count)
            {
                _currentHighlightedTable = _matchingLocalTables[_activeLocalFrameIndex];
            }
        }
    }

    private void ApplyActiveTabHighlight(Frame tableFrame)
    {
        // Blue highlight for the active frame (the one that opens on Enter)
        tableFrame.BorderColor = Color.FromArgb("#005F8C"); // Blue for active
        tableFrame.BackgroundColor = Color.FromArgb("#d2e8f8ff"); // Light blue
        tableFrame.HasShadow = true;
        tableFrame.CornerRadius = 10;
        tableFrame.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Color.FromArgb("#005F8C")),
            Offset = new Point(2, 2),
            Radius = 3,
            Opacity = 0.8f
        };
        tableFrame.Scale = 1.08;
    }

    private void ApplyInactiveTabHighlight(Frame tableFrame)
    {
        // Gray highlight for non-active frames when cycling with Tab
        tableFrame.BorderColor = Color.FromArgb("#9E9E9E"); // Gray for inactive
        tableFrame.BackgroundColor = Color.FromArgb("#F5F5F5"); // Light gray background
        tableFrame.HasShadow = true;
        tableFrame.CornerRadius = 10;
        tableFrame.Shadow = new Shadow
        {
            Brush = new SolidColorBrush(Color.FromArgb("#9E9E9E")),
            Offset = new Point(1, 1),
            Radius = 3,
            Opacity = 0.5f
        };
        tableFrame.Scale = 1.0; // No scaling for inactive frames
    }

    private async Task<bool> HasTransaction(Order order)
    {
        return await AppDbContext.ExecuteSafeAsync(async db =>
        {
            return await db.Transactions.AnyAsync(t => t.Order != null && t.Order.Id == order.Id);
        });
    }

    private async Task<bool> HasRefund(Order order)
    {
        return await AppDbContext.ExecuteSafeAsync(async db =>
        {
            return await db.Refunds.AnyAsync(r => r.Order != null && r.Order.Id == order.Id);
        });
    }
}