using System.Collections.ObjectModel;
using felix1.Logic;
using felix1.Data;
using Microsoft.EntityFrameworkCore;

namespace felix1.OrderSection;

public partial class OrderVisual : ContentPage
{
    public ObservableCollection<Article> ListArticles { get; set; } = new();
    public ObservableCollection<OrderItem> OrderItems { get; set; } = new();

    public OrderVisual()
    {
        InitializeComponent();
        BindingContext = this;
        LoadArticles();
    }


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

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Set up key handling for the page
        this.HandlerChanged += OnHandlerChanged;
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (this.Handler?.PlatformView != null)
        {
            // Add platform-specific key handling here if needed
            SetupKeyHandling();
        }
    }

    private void SetupKeyHandling()
    {
        // This method can be expanded for platform-specific key handling
        // For now, we'll use the cell tapped approach with double-tap for editing
    }

    private void OnOrderItemCellTapped(object sender, Syncfusion.Maui.DataGrid.DataGridCellTappedEventArgs e)
    {
        // Store the selected row and column for potential editing
        if (e.RowData is OrderItem)
        {
            // You can implement double-tap to edit logic here
            // For now, we'll add a method that can be called programmatically
        }
    }

    // Method to start editing quantity of the selected row (call this when Enter is pressed)
    public void EditSelectedQuantity()
    {
        if (orderItemsDataGrid.SelectedIndex >= 0)
        {
            // Find the column index for Quantity
            int quantityColumnIndex = 0; // Quantity is the first column
            orderItemsDataGrid.BeginEdit(orderItemsDataGrid.SelectedIndex, quantityColumnIndex);
        }
    }

    // Alternative: Add toolbar buttons for common actions
    private void OnEditQuantityClicked(object sender, EventArgs e)
    {
        EditSelectedQuantity();
    }

    private void OnIncreaseQuantityClicked(object sender, EventArgs e)
    {
        if (orderItemsDataGrid.SelectedIndex >= 0 && orderItemsDataGrid.SelectedIndex < OrderItems.Count)
        {
            var selectedItem = OrderItems[orderItemsDataGrid.SelectedIndex];
            selectedItem.Quantity++;
        }
    }

    private void OnDecreaseQuantityClicked(object sender, EventArgs e)
    {
        if (orderItemsDataGrid.SelectedIndex >= 0 && orderItemsDataGrid.SelectedIndex < OrderItems.Count)
        {
            var selectedItem = OrderItems[orderItemsDataGrid.SelectedIndex];
            if (selectedItem.Quantity > 1)
                selectedItem.Quantity--;
            else
                OrderItems.Remove(selectedItem);
        }
    }

    private void OnRemoveItemClicked(object sender, EventArgs e)
    {
        if (orderItemsDataGrid.SelectedIndex >= 0 && orderItemsDataGrid.SelectedIndex < OrderItems.Count)
        {
            var selectedItem = OrderItems[orderItemsDataGrid.SelectedIndex];
            OrderItems.Remove(selectedItem);
        }
    }

}