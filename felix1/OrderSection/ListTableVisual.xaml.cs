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
            await db.Orders
                .Include(o => o.Table)
                .Include(o => o.Waiter)
                .Where(o => o.Table != null && o.Waiter != null)
                .ToListAsync())
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
            var tableRow = new HorizontalStackLayout
            {
                Spacing = 10,
                HorizontalOptions = LayoutOptions.Center
            };

            foreach (var table in userTables)
            {
                // Get orders associated with this table
                var ordersForTable = AppDbContext.ExecuteSafeAsync(async db =>
                    await db.Orders
                        .Where(o => o.Table != null && o.Table.Id == table.Id)
                        .ToListAsync())
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
                    HorizontalOptions = LayoutOptions.Center,
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
                        HorizontalOptions = LayoutOptions.Center
                    });
                }

                // Wrap everything inside a frame
                var tableFrame = new Frame
                {
                    WidthRequest = 100,
                    HeightRequest = 100,
                    BackgroundColor = Colors.White,
                    CornerRadius = 10,
                    Padding = 8,
                    Content = tableContent,
                    BorderColor = Color.FromArgb("#C7CFDD")
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

    private void OnCreateTableWindowClicked(User user)
    {
        // Get display size
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        //change this to OrderVisual instead
        //var window = new Window(new CreateTableVisual(user));
        var window = new Window(new OrderVisual());
        window.Height = 800;
        window.Width = 1000;

        // Center the window
        window.X = (displayInfo.Width / displayInfo.Density - window.Width) / 2;
        window.Y = (displayInfo.Height / displayInfo.Density - window.Height) / 2;
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
    }
}