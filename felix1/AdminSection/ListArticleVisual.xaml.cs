using System.Collections.ObjectModel;
using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Maui.DataGrid;

namespace felix1;

public partial class ListArticleVisual : ContentView
{
    public static ListArticleVisual? Instance { get; private set; }

    public ObservableCollection<Article> Articles { get; set; } = new();

    public ListArticleVisual()
    {
        InitializeComponent();
        BindingContext = this;
        Instance = this;
        LoadArticles();
    }

    private void LoadArticles()
    {
        var articlesFromDb = AppDbContext.ExecuteSafeAsync(async db =>
            await db.Articles
                .Where(a => !a.IsDeleted)
                .ToListAsync())
            .GetAwaiter().GetResult();

        Articles.Clear();
        foreach (var article in articlesFromDb)
            Articles.Add(article);
    }

    public void ReloadArticles() // PUBLIC method to allow external refresh
    {
        LoadArticles();
    }

    private void OnCreateArticleWindowClicked(object sender, EventArgs e)
    {
        // Get display size
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;

        var window = new Window(new CreateArticleVisual());

        window.Height = 700;
        window.Width = 800;

        // Center the window
        window.X = (displayInfo.Width / displayInfo.Density - window.Width) / 2;
        window.Y = (displayInfo.Height / displayInfo.Density - window.Height) / 2;

        Application.Current?.OpenWindow(window);
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        var button = (ImageButton)sender;
        var article = (Article)button.BindingContext;

        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        var window = new Window(new CreateArticleVisual(article))
        {
            Height = 700,
            Width = 800,
            X = (displayInfo.Width / displayInfo.Density - 800) / 2,
            Y = (displayInfo.Height / displayInfo.Density - 700) / 2
        };

        Application.Current?.OpenWindow(window);
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.BindingContext is Article article)
        {
            if (Application.Current?.MainPage != null)
            {
                bool answer = await Application.Current.MainPage.DisplayAlert(
                    "Confirmacion",
                    $"Estas seguro de que desea eliminar '{article.Name}'?",
                    "Si", "No");

                if (answer)
                {
                    await AppDbContext.ExecuteSafeAsync(async db =>
                    {
                        var articleToDelete = await db.Articles.FindAsync(article.Id);

                        if (articleToDelete != null)
                        {
                            articleToDelete.IsDeleted = true;
                            await db.SaveChangesAsync();
                            LoadArticles();
                            
                            //for what is this code below?
                            //Device.BeginInvokeOnMainThread(LoadArticles);
                        }
                    });
                }
            }
        }
    }

    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            // Reset the DataGrid to show all articles
            dataGrid.ItemsSource = Articles;
        }
        else
        {
            // Filter the collection
            dataGrid.ItemsSource = Articles
                .Where(a => a.Name != null && a.Name.ToLower().Contains(searchText))
                .ToList();
        }
    }
}