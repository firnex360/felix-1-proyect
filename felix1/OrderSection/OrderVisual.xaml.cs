using System.Collections.ObjectModel;
using felix1.Logic;
using felix1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Windows.System;
using Syncfusion.Maui.DataGrid;
using Syncfusion.Maui.Core.Internals;
using Syncfusion.Maui.DataGrid.Helper;



#if WINDOWS
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
#endif

namespace felix1.OrderSection;

public partial class OrderVisual : ContentPage
{
    public ObservableCollection<Article> ListArticles { get; set; } = new();
    public ObservableCollection<OrderItem> OrderItems { get; set; } = new();

    private bool _isEditing = false;

    public OrderVisual()
    {
        InitializeComponent();
        BindingContext = this;
        LoadArticles();
        orderItemsDataGrid.CurrentCellBeginEdit += (s, e) => _isEditing = true;
        orderItemsDataGrid.CurrentCellEndEdit += (s, e) => _isEditing = false;
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

    private void OnArticleCellTapped(object sender, Syncfusion.Maui.DataGrid.DataGridCellTappedEventArgs e)
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
        orderItemsDataGrid.SelectionController = new CustomRowSelectionController(orderItemsDataGrid, this);
        listArticleDataGrid.SelectionController = new CustomArticleSelectionController(listArticleDataGrid, this);
        
        // Ensure handler for both grids are set on appearing
        OnHandlerChanged(this, EventArgs.Empty);
    }

    private void OnHandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        var platformView = orderItemsDataGrid.Handler?.PlatformView as Microsoft.UI.Xaml.FrameworkElement;
        if (platformView != null)
        {
            platformView.KeyDown -= PlatformView_KeyDown;
            platformView.KeyDown += PlatformView_KeyDown;
        }
#endif
    }

#if WINDOWS
    private void PlatformView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Enter:
                //OnEditQuantityClicked(this, EventArgs.Empty);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Add:
                OnIncreaseQuantityClicked(this, EventArgs.Empty);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Subtract:
                OnDecreaseQuantityClicked(this, EventArgs.Empty);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Delete:
                OnRemoveItemClicked(this, EventArgs.Empty);
                e.Handled = true;
                break;
        }
    }
#endif


    public class CustomRowSelectionController : DataGridRowSelectionController
    {
        private readonly OrderVisual _parent;
        public CustomRowSelectionController(SfDataGrid dataGrid, OrderVisual parent) : base(dataGrid)
        {
            _parent = parent;
        }
        protected override void ProcessKeyDown(KeyEventArgs args, bool isCtrlKeyPressed, bool isShiftKeyPressed)
        {
            if (args.Key == KeyboardKey.Enter)
            {
                if (_parent._isEditing)
                {
                    // If already editing, treat Enter as Tab (move to next cell)
                    var tabArgs = new KeyEventArgs(KeyboardKey.Tab) { Handled = false };
                    base.ProcessKeyDown(tabArgs, isCtrlKeyPressed, isShiftKeyPressed);
                    args.Handled = true;
                }
                else
                {
                    int selectedIndex = DataGrid != null ? DataGrid.SelectedIndex : -1;
                    if (selectedIndex >= 0 && selectedIndex <= _parent.OrderItems.Count)
                    {
                        var selectedItem = _parent.OrderItems[selectedIndex - 1];
                        _parent.EditOrderItemQuantity(selectedItem);
                        args.Handled = true;
                    }
                }
            }
            else
            {
                base.ProcessKeyDown(args, isCtrlKeyPressed, isShiftKeyPressed);
            }
        }
    }

    public class CustomArticleSelectionController : DataGridRowSelectionController
    {
        private readonly OrderVisual _parent;
        public CustomArticleSelectionController(SfDataGrid dataGrid, OrderVisual parent) : base(dataGrid)
        {
            _parent = parent;
        }
        protected override void ProcessKeyDown(KeyEventArgs args, bool isCtrlKeyPressed, bool isShiftKeyPressed)
        {
            if (args.Key == KeyboardKey.Enter)
            {
                int selectedIndex = DataGrid != null ? DataGrid.SelectedIndex : -1;
                if (selectedIndex >= 0 && selectedIndex <= _parent.ListArticles.Count)
                {
                    var selectedArticle = _parent.ListArticles[selectedIndex - 1];
                    _parent.AddArticleToOrder(selectedArticle);
                    args.Handled = true;
                }
            }
            else
            {
                base.ProcessKeyDown(args, isCtrlKeyPressed, isShiftKeyPressed);
            }
        }
    }


    //methods for button actions

    //method to start editing quantity of the selected row (deprecated)
    [Obsolete("Use EditOrderItemQuantity method instead.")]
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

    //new method to edit a specific OrderItem
    public void EditOrderItemQuantity(OrderItem item)
    {
        int rowIndex = OrderItems.IndexOf(item) + 1; // +1 because the first row is empty in the DataGrid
        int quantityColumnIndex = 0;
        if (rowIndex >= 0)
            orderItemsDataGrid.BeginEdit(rowIndex, quantityColumnIndex);
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