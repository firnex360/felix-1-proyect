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
        labeltest.Text = "Button clicked";
    }

}