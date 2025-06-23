
using felix1.Data;

namespace felix1.OrderSection;

public partial class ListOrderVisual : ContentView
{
    public static ListOrderVisual? Instance { get; private set; }

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

        foreach (var user in meseros)
        {
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
                            VerticalOptions = LayoutOptions.Center
                            // CLICK EVENT TO CREATE TABLE
                        }
                    }
                }
            };

            MeseroContainer.Children.Add(card);
        }
    }
}