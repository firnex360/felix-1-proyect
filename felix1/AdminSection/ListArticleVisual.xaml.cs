using System.Collections.ObjectModel;
using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;

namespace felix1;

public partial class ListArticleVisual : ContentView
{

    public ObservableCollection<Article> Articles { get; set; } = new();

    public ListArticleVisual()
    {
        InitializeComponent();
        BindingContext = this;
        LoadArticles();
    }

    private void LoadArticles()
    {
        using var db = new AppDbContext();
        var articlesFromDb = db.Articles != null
            ? db.Articles.Where(a => !a.IsDeleted)
            .ToList()
            : new List<Article>();

        Articles.Clear();
        foreach (var article in articlesFromDb)
            Articles.Add(article);
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


    private void OnViewClicked(object sender, EventArgs e)
    {
        // Dummy function for View button
        labeltest.Text = "View clicked";
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
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

    private void OnDeleteClicked(object sender, EventArgs e)
    {
        // Dummy function for Delete button
        labeltest.Text = "Delete clicked";
    }

}