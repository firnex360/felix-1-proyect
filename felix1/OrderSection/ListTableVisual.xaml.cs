using System.Collections.ObjectModel;
using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;

namespace felix1.OrderSection;

public partial class ListOrderVisual : ContentView
{
    public static ListOrderVisual? Instance { get; private set; }
    public ObservableCollection<Table> Tables { get; set; } = new();

    public ListOrderVisual()
    {
        InitializeComponent();
        BindingContext = this;
        Instance = this;
        LoadMeseros();
    }

    private Color GetOrderButtonColor(Order order)
    {
        if (order.IsBillRequested)
        {
            return Color.FromArgb("#4CAF50"); // CUANDO YA IMPRIMES  COLOR TEMPORAL
        }
        else
        {
            return Color.FromArgb("#2196F3"); // CUANDO ACABAS DE GENERAR LA ORDEN  COLOR TEMPORAL
        }
    }

    private void LoadMeseros()
    {
        var meseros = AppDbContext.ExecuteSafeAsync(async db =>
            await db.Users
                .Where(u => u.Role == "Mesero" && !u.Deleted && u.Available)
                .ToListAsync())
            .GetAwaiter().GetResult();

        var tableOrders = AppDbContext.ExecuteSafeAsync(async db =>
        {
            var openCashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);
            if (openCashRegister == null) return new List<Order>();

            // Load orders with all required relationships
            var orders = await db.Orders
                .Include(o => o.Table)
                .Include(o => o.Waiter)
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Article)
                .Where(o => o.Table != null &&
                           o.Waiter != null &&
                           o.CashRegister == openCashRegister &&
                           !o.IsDuePaid) // Only show unpaid orders
                .ToListAsync();

            return orders;
        })
        .GetAwaiter().GetResult();

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
                    VerticalOptions = LayoutOptions.Center
                };

                // Table number label
                tableContent.Children.Add(new Label
                {
                    Text = $"Mesa #{table.LocalNumber}",
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 14,
                    HorizontalOptions = LayoutOptions.Start,
                    TextColor = Colors.Black
                });

                // Add buttons for each order
                foreach (var order in ordersForTable)
                {
                    var orderButton = new Button
                    {
                        Text = $"Orden #{order.OrderNumber}",
                        FontSize = 12,
                        HeightRequest = 30,
                        WidthRequest = 90,
                        BackgroundColor = order.IsBillRequested ?
                            Color.FromArgb("#4CAF50") : // Green for printed orders
                            Color.FromArgb("#2196F3"),  // Blue for regular orders
                        TextColor = Colors.White,
                        CornerRadius = 5,
                        HorizontalOptions = LayoutOptions.Start,
                        Command = new Command(() => OnViewOrderClicked(order))
                    };
                    tableContent.Children.Add(orderButton);
                }

                // Determine frame border color based on orders
                Color frameBorderColor = ordersForTable.Any() ?
                    (ordersForTable.First().IsBillRequested ?
                        Color.FromArgb("#4CAF50") :
                        Color.FromArgb("#2196F3")) :
                    Color.FromArgb("#C7CFDD"); // Default color if no orders

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
    private async void OnCreateTableWindowClicked(User user)
    {
        Order? order = null;

        await AppDbContext.ExecuteSafeAsync(async db =>
        {
            var cashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);
            if (cashRegister == null)
            {
                await Application.Current!.MainPage!.DisplayAlert("Error", "No hay una caja abierta.", "OK");
                return;
            }

            int orderNumber = await db.Orders
                .Where(o => o.CashRegister != null && o.CashRegister.Id == cashRegister.Id)
                .CountAsync();

            // ALL TABLES IN THE **OPEN** CASH REGISTER
            var allTables = await db.Orders
                .Include(o => o.Table)
                .Include(o => o.Waiter)
                .Where(o => o.CashRegister != null && o.CashRegister.Id == cashRegister.Id && o.Table != null)
                .Select(o => o.Table!)
                //.Distinct()
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
                LocalNumber = 4,
                GlobalNumber = 3,
                IsTakeOut = false,
                IsBillRequested = false,
                IsPaid = false
            };

            db.Tables.Add(table);
            await db.SaveChangesAsync();
        });

        await AppDbContext.ExecuteSafeAsync(async db =>
        {
            var savedTable = await db.Tables.FirstOrDefaultAsync(t => t.Id == table!.Id);
            var cashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);

            if (savedTable == null || cashRegister == null)
            {
                await Application.Current!.MainPage!.DisplayAlert("Error", "Datos no válidos al crear la orden.", "OK");
                return;
            }

            db.Attach(waiter!);
            db.Attach(cashRegister);

            var order = new Order
            {
                OrderNumber = orderNumber + 1,
                Date = DateTime.Now,
                Waiter = await db.Users.FindAsync(user.Id),
                Table = table,
                Items = new List<OrderItem>(),
                CashRegister = cashRegister,
                IsDuePaid = false,
                IsBillRequested = false
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();
        });

        if (order != null)
        {
            order = await AppDbContext.ExecuteSafeAsync(async db =>
                await db.Orders
                    .Include(o => o.Table)
                    .Include(o => o.Waiter)
                    .Include(o => o.Items)
                    .ThenInclude(oi => oi.Article)
                    .FirstOrDefaultAsync(o => o.Id == order.Id));

            OnViewOrderClicked(order);
        }

        ReloadTM();
    }

private void AddTakeoutOrderToPanel(Order order)
{
    int displayOrderNumber = order.Table?.LocalNumber ?? order.OrderNumber;

    var orderButton = new Button
    {
        Text = $"Orden #{displayOrderNumber}",
        FontSize = 14,
        HeightRequest = 40,
        BackgroundColor = Color.FromArgb("#C7CFDD"),
        TextColor = Colors.White,
        CornerRadius = 6,
        HorizontalOptions = LayoutOptions.Fill,
        Margin = new Thickness(0, 5),
        Command = new Command(() => OnViewOrderClicked(order))
    };

    TakeoutPanel.Children.Add(orderButton);
}

private void LoadExistingTakeoutOrders()
{
    var takeoutOrders = AppDbContext.ExecuteSafeAsync(async db =>
    {
        var openCashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);
        if (openCashRegister == null) return new List<Order>();

        var orders = await db.Orders
            .Include(o => o.Table)
            .Where(o => o.CashRegister!.Id == openCashRegister.Id &&
                        o.Table != null &&
                        o.Table.IsTakeOut)
            .ToListAsync();

        foreach (var order in orders)
        {
            order.Items = await db.OrderItems
                .Include(oi => oi.Article)
                .Where(oi => EF.Property<int>(oi, "OrderId") == order.Id)
                .ToListAsync();
        }

        return orders;
    }).GetAwaiter().GetResult();

    // Only add buttons that aren't already there (basic duplicate check by text)
    foreach (var order in takeoutOrders)
    {
        int displayOrderNumber = order.Table?.LocalNumber ?? order.OrderNumber;
        string buttonText = $"Orden #{displayOrderNumber}";

        bool alreadyExists = TakeoutPanel.Children
            .OfType<Button>()
            .Any(b => b.Text == buttonText);

        if (!alreadyExists)
        {
            AddTakeoutOrderToPanel(order);
        }
    }
}
private async void OnCreateTakeoutOrderClicked(object sender, EventArgs e)
{
    await AppDbContext.ExecuteSafeAsync(async db =>
    {
        var waiter = await db.Users.FirstOrDefaultAsync(u => u.Name == "TAKEOUT");
        var cashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);

        if (cashRegister == null)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", "No hay una caja abierta.", "OK");
            return;
        }

        var existingTakeouts = await db.Orders
            .Where(o => o.CashRegister!.Id == cashRegister.Id && o.Table != null && o.Table.IsTakeOut)
            .Select(o => o.Table!)
            .ToListAsync();

        int nextTakeoutNumber = -(existingTakeouts.Count + 1);

        var table = new Table
        {
            LocalNumber = nextTakeoutNumber,
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

        AddTakeoutOrderToPanel(order); 
        OnViewOrderClicked(order);
    });
}

    private async void OnViewOrderClicked(Order order)
    {
        var loadedOrder = await AppDbContext.ExecuteSafeAsync(async db =>
            await db.Orders
                .Include(o => o.Table)
                .Include(o => o.Waiter)
                .Include(o => o.Items)
                .ThenInclude(oi => oi.Article)
                .FirstOrDefaultAsync(o => o.Id == order.Id));

        if (loadedOrder == null)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "No se pudo cargar la orden.", "OK");
            return;
        }

        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        ContentPage targetPage = loadedOrder.IsBillRequested
            ? new PaymentVisual(loadedOrder)
            : new OrderVisual(loadedOrder);

        var window = new Window(targetPage)
        {
            Height = 700,
            Width = 1000,
            X = (displayInfo.Width / displayInfo.Density - 1000) / 2,
            Y = ((displayInfo.Height / displayInfo.Density - 700) / 2) - 25
        };

        Application.Current?.OpenWindow(window);
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
}