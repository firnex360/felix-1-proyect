using System.Collections.ObjectModel;
using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Maui.DataGrid;
using Syncfusion.Maui.Popup;

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
        // Create the popup
        var popup = new SfPopup
        {
            WidthRequest = 1000,
            HeightRequest = 700,
            ShowFooter = false,
            ShowCloseButton = true,
            ShowHeader = false,
            StaysOpen = true,
            PopupStyle = new PopupStyle
            {
                MessageBackground = Colors.White,
                HeaderBackground = Colors.Transparent,
                HeaderTextColor = Colors.Black,
                CornerRadius = new CornerRadius(10)
            }
        };

        // Use the converted CreateArticleVisual (now a ContentView)
        var createArticleView = new CreateArticleVisual();
        
        // Try setting content directly without DataTemplate first
        try 
        {
            // Some versions of Syncfusion popup support direct content assignment
            var contentProperty = popup.GetType().GetProperty("Content");
            if (contentProperty != null && contentProperty.CanWrite)
            {
                contentProperty.SetValue(popup, createArticleView);
                createArticleView.SetPopupReference(popup);
            }
            else
            {
                // Fallback to ContentTemplate
                createArticleView.SetPopupReference(popup);
                popup.ContentTemplate = new DataTemplate(() => createArticleView);
            }
        }
        catch
        {
            // Fallback to ContentTemplate
            createArticleView.SetPopupReference(popup);
            popup.ContentTemplate = new DataTemplate(() => createArticleView);
        }

        // Handle when popup is closed to reload articles
        popup.Closed += (s, args) =>
        {
            ReloadArticles();
        };

        // Show the popup
        popup.Show();
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        var button = (ImageButton)sender;
        var article = (Article)button.BindingContext;

        // Create the popup for editing
        var popup = new SfPopup
        {
            WidthRequest = 1000,
            HeightRequest = 700,
            ShowFooter = false,
            ShowHeader = false,
            ShowCloseButton = true,
            StaysOpen = true,
            PopupStyle = new PopupStyle
            {
                MessageBackground = Colors.White,
                HeaderBackground = Colors.Transparent,
                HeaderTextColor = Colors.Black,
                CornerRadius = new CornerRadius(10)
            }
        };

        // Use the converted CreateArticleVisual (now a ContentView) with the article to edit
        var createArticleView = new CreateArticleVisual(article);
        createArticleView.SetPopupReference(popup);
        
        // Set the ContentView directly as content
        popup.ContentTemplate = new DataTemplate(() => createArticleView);

        // Handle when popup is closed to reload articles
        popup.Closed += (s, args) =>
        {
            ReloadArticles();
        };

        // Show the popup
        popup.Show();
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