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
        var articlesFromDb = db.Articles
            .Where(a => !a.IsDeleted)
            .ToList();

        Articles.Clear();
        foreach (var article in articlesFromDb)
            Articles.Add(article);
    }

    private void OnCreateArticleClicked(object sender, EventArgs e)
    {
        // var window = new Window(new CreateArticleVisual()); // or MainPage()
        // return window;
    }

    private void OnViewClicked(object sender, EventArgs e)
    {
        // Dummy function for View button
        labeltest.Text = "View clicked";
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        // Dummy function for Edit button
        labeltest.Text = "Edit clicked";
    }

    private void OnDeleteClicked(object sender, EventArgs e)
    {
        // Dummy function for Delete button
        labeltest.Text = "Delete clicked";
    }

}