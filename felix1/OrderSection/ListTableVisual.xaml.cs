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
            
            var orders = await db.Orders
                .Include(o => o.Table)
                .Include(o => o.Waiter)
                .Where(o => o.Table != null && o.Waiter != null && o.CashRegister == openCashRegister)
                .ToListAsync();

            // Load OrderItems separately for each order
            foreach (var order in orders)
            {
                order.Items = await db.OrderItems
                    .Include(oi => oi.Article)
                    .Where(oi => EF.Property<int>(oi, "OrderId") == order.Id)
                    .ToListAsync();
            }

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

            //mesero's name
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
                // Get orders associated with this table
                var ordersForTable = AppDbContext.ExecuteSafeAsync(async db =>
                {
                    var orders = await db.Orders
                        .Where(o => o.Table != null && o.Table.Id == table.Id)
                        .ToListAsync();

                    // Load OrderItems for each order
                    foreach (var order in orders)
                    {
                        order.Items = await db.OrderItems
                            .Include(oi => oi.Article)
                            .Where(oi => EF.Property<int>(oi, "OrderId") == order.Id)
                            .ToListAsync();
                    }

                    return orders;
                })
                .GetAwaiter().GetResult();

                // Create inner vertical stack for label + order buttons
                var tableContent = new VerticalStackLayout
                {
                    Spacing = 5,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                // Add label for table number
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
                    tableContent.Children.Add(new Button
                    {
                        Text = $"Orden #{order.OrderNumber}",
                        FontSize = 12,
                        HeightRequest = 30,
                        WidthRequest = 90,
                        BackgroundColor = Color.FromArgb("#C7CFDD"),
                        TextColor = Colors.White,
                        CornerRadius = 5,
                        HorizontalOptions = LayoutOptions.Start,
                        Command = new Command(() => OnViewOrderClicked(order)) // CLICK EVENT
                    });
                }

                // Wrap everything inside a frame
                var tableFrame = new Frame
                {
                    HeightRequest = 100,
                    BackgroundColor = Colors.White,
                    CornerRadius = 10,
                    Padding = 8,
                    Content = tableContent,
                    BorderColor = Color.FromArgb("#C7CFDD"),
                    HorizontalOptions = LayoutOptions.Fill
                };

                tableRow.Children.Add(tableFrame);
            }
            // Add the row to the main stack
            stack.Children.Add(tableRow);

            stack.Children.Add(new Button
            {
                Text = "Crear Mesa",
                BackgroundColor = Color.FromArgb("#005F8C"),
                TextColor = Colors.White,
                CornerRadius = 8,
                HeightRequest = 40,
                WidthRequest = 120,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Command = new Command(() => OnCreateTableWindowClicked(user)) // CLICK EVENT 
            });

            var card = new Frame
            {
                WidthRequest = 300,
                CornerRadius = 10,
                Padding = 10,
                BackgroundColor = Colors.White,
                BorderColor = Colors.White,
                VerticalOptions = LayoutOptions.Fill,
                Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.2f },
                Content = stack,
            };

            MeseroContainer.Children.Add(card);
        }
    }

    private async void OnCreateTableWindowClicked(User user)
    {


        /*var cashRegister = await AppDbContext.ExecuteSafeAsync(async db =>
            await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen));*/

        
        /*if (cashRegister == null)
        {
            await Application.Current?.MainPage.DisplayAlert("Error", "No hay una caja abierta.", "OK");
            return;
        }*/
        //ORDER NUMBER
        /*var orderNumber = await AppDbContext.ExecuteSafeAsync(async db => await db.Orders
                .Where(o => o.CashRegister != null && o.CashRegister.Id == cashRegister.Id)
                .CountAsync());*/


        var waiter = await AppDbContext.ExecuteSafeAsync(async db =>
            await db.Users.FindAsync(user.Id));

        /*if (waiter == null)
        {
            await Application.Current?.MainPage.DisplayAlert("Error", "Mesero no encontrado.", "OK");
            return;
        }*/

        Table? table = null;
        int orderNumber = 0;
        await AppDbContext.ExecuteSafeAsync(async db =>
        {
            /*
            if (cashRegister == null)
            {
                await Application.Current?.MainPage.DisplayAlert("Error", "No hay una caja abierta.", "OK");
                return;
            }*/
            var cashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen);
            if (cashRegister == null)
            {
                await Application.Current!.MainPage!.DisplayAlert("Error", "No hay una caja abierta.", "OK");
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
                LocalNumber = waiterTables.Count + 1,
                GlobalNumber = allTables.Count + 1,
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
                Waiter = waiter, //comes as a parameter from the list, depending where we clicked
                Table = savedTable, //automatically created before a new order
                Items = null,
                CashRegister = cashRegister,  //we can only work with the open cash register
                IsDuePaid = false,
                IsBillRequested = false
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();
        OnViewOrderClicked(order);
        });

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

    private void OnViewOrderClicked(Order order)
    {
        // Get display size
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;

        //var window = new Window(new CreateTableVisual(user));
        var window = new Window(new OrderVisual(order));
        window.Height = 700;
        window.Width = 1000;

        // Center the window
        window.X = (displayInfo.Width / displayInfo.Density - window.Width) / 2;
        window.Y = ((displayInfo.Height / displayInfo.Density - window.Height) / 2) - 25; // Add some offset for better visibility
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

    public void ReloadTM() // PUBLIC method to allow external refresh
    {
        LoadTables();
        LoadMeseros();
        LoadExistingTakeoutOrders();
    }
}