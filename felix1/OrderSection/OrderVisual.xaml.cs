using System.Collections.ObjectModel;
using felix1.Logic;
using felix1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Windows.System;

#if WINDOWS
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
#endif

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
        this.HandlerChanged += OnHandlerChanged;
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        var platformView = this.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
        if (platformView != null)
        {
            platformView.KeyDown -= PlatformView_KeyDown;
            platformView.KeyDown += PlatformView_KeyDown;
        }
#endif
    }

#if WINDOWS
    private void PlatformView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Enter: //doesnt work right yet
                OnEditQuantityClicked(this, EventArgs.Empty);
                break;
            case Windows.System.VirtualKey.Add:
                OnIncreaseQuantityClicked(this, EventArgs.Empty);
                break;
            case Windows.System.VirtualKey.Subtract:
                OnDecreaseQuantityClicked(this, EventArgs.Empty);
                break;
            case Windows.System.VirtualKey.Delete:
                OnRemoveItemClicked(this, EventArgs.Empty);
                break;
        }
    }
#endif

    //methods for button actions

    //method to start editing quantity of the selected row
    private void OnEditQuantityClicked(object sender, EventArgs e)
    {
        if (orderItemsDataGrid.SelectedIndex >= 0)
        {
            // Find the column index for Quantity
            int rowIndex = orderItemsDataGrid.SelectedIndex;
            int quantityColumnIndex = 0;
            orderItemsDataGrid.BeginEdit(rowIndex, quantityColumnIndex);
        }
    }

    private void OnIncreaseQuantityClicked(object sender, EventArgs e)
    {
        var selectedItemIndex = orderItemsDataGrid.SelectedIndex;

        if (selectedItemIndex >= 0 && selectedItemIndex <= OrderItems.Count)
        {
            var selectedItem = OrderItems[selectedItemIndex - 1];
            selectedItem.Quantity++;
        }
    }

    private void OnDecreaseQuantityClicked(object sender, EventArgs e)
    {
        var selectedItemIndex = orderItemsDataGrid.SelectedIndex;

        if (selectedItemIndex >= 0 && selectedItemIndex <= OrderItems.Count)
        {
            var selectedItem = OrderItems[selectedItemIndex - 1];
            if (selectedItem.Quantity > 1)
                selectedItem.Quantity--;
            else
                OrderItems.Remove(selectedItem);
        }
    }

    private void OnRemoveItemClicked(object sender, EventArgs e)
    {
        var selectedItemIndex = orderItemsDataGrid.SelectedIndex;

        if (selectedItemIndex >= 0 && selectedItemIndex <= OrderItems.Count)
        {
            var selectedItem = OrderItems[selectedItemIndex - 1];
            OrderItems.Remove(selectedItem);
        }
    }

}