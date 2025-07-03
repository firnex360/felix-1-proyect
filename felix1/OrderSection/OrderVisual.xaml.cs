using System.Collections.ObjectModel;
using felix1.Logic;
using felix1.Data;
using Microsoft.EntityFrameworkCore;

namespace felix1.OrderSection;

public partial class OrderVisual : ContentPage
{
    public ObservableCollection<Article> ListArticles { get; set; } = new();
    public ObservableCollection<OrderItem> OrderItems { get; set; } = new();

    private void LoadArticles()
    {
        var articlesFromDb = AppDbContext.ExecuteSafeAsync(async db =>
            await db.Articles
                .Where(a => !a.IsDeleted)
                .ToListAsync())
            .GetAwaiter().GetResult();

        ListArticles.Clear();
        foreach (var article in articlesFromDb)
            ListArticles.Add(article);
    }

    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            // Reset the DataGrid to show all articles
            listArticleDataGrid.ItemsSource = ListArticles;
        }
        else
        {
            // Filter the collection
            listArticleDataGrid.ItemsSource = ListArticles
                .Where(a => a.Name != null && a.Name.ToLower().Contains(searchText))
                .ToList();
        }
    }

    private void OnArticleCellDoubleTapped(object sender, Syncfusion.Maui.DataGrid.DataGridCellDoubleTappedEventArgs e)
    {
        if (e.RowData is Article selectedArticle)
        {
            AddArticleToOrder(selectedArticle);
        }
    }

    private void AddArticleToOrder(Article article)
    {
        // Check if the article is already in the order
        var existingOrderItem = OrderItems.FirstOrDefault(oi => oi.Article?.Id == article.Id);

        if (existingOrderItem != null)
        {
            // If it exists, increase the quantity
            existingOrderItem.Quantity++;
        }
        else
        {
            // Create new order item with default quantity of 1
            var newOrderItem = new OrderItem
            {
                Article = article,
                Quantity = 1,
                UnitPrice = (decimal)article.PriPrice
            };

            OrderItems.Add(newOrderItem);
        }
    }

    public OrderVisual()
    {
        InitializeComponent();
        BindingContext = this;
        LoadArticles();
    }
}