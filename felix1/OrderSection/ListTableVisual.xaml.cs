
using System.Collections.ObjectModel;
using felix1.Data;
using felix1.Logic;

namespace felix1.OrderSection;

public partial class ListOrderVisual : ContentView
{
    public static ListOrderVisual? Instance { get; private set; }
    public ObservableCollection<Table> Tables { get; set; } = new();

    [Obsolete]
    public ListOrderVisual()
    {
        InitializeComponent();
        BindingContext = this;
        Instance = this;
        LoadMeseros();
    }

    [Obsolete] //to delete the warning in verticaloptions
    private void LoadMeseros()
    {
        using var db = new AppDbContext();
        var meseros = db.Users
            .Where(u => u.Role == "Mesero" && !u.Deleted && u.Available)
            .ToList();
/*
        var tableOrders = db.Orders
            .Where(o => o.Table != null && o.Waiter != null)
            .ToList();
*/
        MeseroContainer.Children.Clear();

        foreach (var user in meseros)
        {
/*
        var userTables = tableOrders
            .Where(o => o.Waiter?.Id == user.Id && o.Table != null)
            .Select(o => o.Table!)
            .Distinct()
            .ToList();

            var stack = new VerticalStackLayout
            {
                Spacing = 12,
                VerticalOptions = LayoutOptions.Start,
                Children =
                {
                    new Label
                    {
                        Text = user.Name,
                        FontSize = 18,
                        TextColor = Colors.Black,
                        FontAttributes = FontAttributes.Bold,
                        HorizontalOptions = LayoutOptions.Center
                    }
                }
            };


            // Add existing table buttons
            foreach (var table in userTables)
            {
                stack.Children.Add(new Button
                {
                    Text = $"Mesa #{table.Id}",
                    BackgroundColor = Colors.LightGreen,
                    TextColor = Colors.White,
                    CornerRadius = 8,
                    HeightRequest = 40,
                    WidthRequest = 120,
                    HorizontalOptions = LayoutOptions.Center
                });
            }
            */
            var card = new Frame
            {
                WidthRequest = 300,
                CornerRadius = 10,
                Padding = 10,
                BackgroundColor = Colors.White,
                BorderColor = Colors.White,
                VerticalOptions = LayoutOptions.FillAndExpand,
                Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.2f },
                Content = new VerticalStackLayout
                {
                    Spacing = 12,
                    VerticalOptions = LayoutOptions.Start,
                    Children =
                    {
                        new Label
                        {
                            Text = user.Name,
                            FontSize = 18,
                            TextColor = Colors.Black,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Button
                        {
                            Text = "Crear Mesa",
                            BackgroundColor = Color.FromArgb("LightBlue"),
                            TextColor = Colors.White,
                            CornerRadius = 8,
                            HeightRequest = 40,
                            WidthRequest = 120,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            Command = new Command(() => OnCreateTableWindowClicked(user)) // CLICK EVENT TO CREATE TABLE
                        }
                    }
                }
            };

            MeseroContainer.Children.Add(card);
        }
    }

    //TO CREATE A TABLE WE NEED AT LEAST ONE ORDER
    private void CreateOrder(User mesero)
    {
        using var db = new AppDbContext();
/*
        var tableButton = new Button
        {
            Text = $"Mesa #{table.Id}",
            BackgroundColor = Color.FromArgb("LightGreen"),
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 40,
            WidthRequest = 120,
            HorizontalOptions = LayoutOptions.Center
        };
        
        var card = MeseroContainer.Children
            .OfType<Frame>()
            .FirstOrDefault(f => f.Content is VerticalStackLayout layout &&
                                layout.Children.OfType<Label>().FirstOrDefault()?.Text == mesero.Name);

        if (card != null && card.Content is VerticalStackLayout stack)
        {
            stack.Children.Add(tableButton);
        }
*/

    }

    private void OnCreateTableWindowClicked(User user)
    {
        // Get display size
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;

        var window = new Window(new CreateTableVisual(user));

        window.Height = 700;
        window.Width = 800;

        // Center the window
        window.X = (displayInfo.Width / displayInfo.Density - window.Width) / 2;
        window.Y = (displayInfo.Height / displayInfo.Density - window.Height) / 2;

        Application.Current?.OpenWindow(window);
    }


    private void LoadTables()
    {
        using var db = new AppDbContext();
        var tablesFromDb = db.Tables != null
            ? db.Tables.ToList()
            : new List<Table>();

        Tables.Clear();
        foreach (var table in tablesFromDb)
            Tables.Add(table);
    }


    public void ReloadTables() // PUBLIC method to allow external refresh
    {
        LoadTables();
    }



}