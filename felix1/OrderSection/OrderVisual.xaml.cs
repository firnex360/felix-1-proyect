using System.Collections.ObjectModel;
using felix1.Logic;
using felix1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Syncfusion.Maui.DataGrid;
using Syncfusion.Maui.Core.Internals;
using Syncfusion.Maui.DataGrid.Helper;
using Microsoft.Maui.Controls;
using System.Drawing;
using System.Drawing.Printing;
using Scriban;

#if WINDOWS
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
#endif

namespace felix1.OrderSection;

public partial class OrderVisual : ContentPage
{
    public ObservableCollection<Article> ListArticles { get; set; } = new();
    public ObservableCollection<OrderItem> OrderItems { get; set; } = new();
    private Order? _currentOrder;
    private bool _isEditing = false;
    private bool _useSecondaryPrice = false; // Cache for cash register setting

    // Constants for testing
    private const decimal TAX_RATE = 0.18m; // 18% tax rate
    private decimal _discountAmount = 0m;


    public OrderVisual(Order order)
    {
        InitializeComponent();
        BindingContext = this;
        _currentOrder = order;
        LoadCashRegisterSettings(); // Load cash register settings first
        LoadArticles();
        orderItemsDataGrid.CurrentCellBeginEdit += (s, e) => _isEditing = true;
        orderItemsDataGrid.CurrentCellEndEdit += (s, e) =>
        {
            _isEditing = false;

            var rowIndex = e.RowColumnIndex.RowIndex;
            var columnIndex = e.RowColumnIndex.ColumnIndex;

            if (rowIndex <= 0 || rowIndex > OrderItems.Count)
                return;

            var column = orderItemsDataGrid.Columns[columnIndex];
            if (column.MappingName != "Quantity")
                return;

            var item = OrderItems[rowIndex - 1];

            // Obtener el valor actual de Quantity por reflexi�n
            var quantityProp = item.GetType().GetProperty("Quantity");
            if (quantityProp == null) return;

            var value = quantityProp.GetValue(item);
            if (value is int quantity && quantity < 0)
            {
                quantityProp.SetValue(item, 0); // Sobrescribir con 0
                orderItemsDataGrid.View?.Refresh(); // Refrescar el grid
                UpdateOrderTotals();
            }
        };

        OrderItems.CollectionChanged += (s, e) => UpdateOrderTotals();

        if (_currentOrder != null)
        {
            LoadOrderDetails(_currentOrder);
            _discountAmount = order.Discount;
            discountEntry.Text = _discountAmount.ToString();

            dueToPayCheckBox.IsChecked = order.IsDuePaid;
        }

    }

    private void LoadCashRegisterSettings()
    {
        var cashRegister = AppDbContext.ExecuteSafeAsync(async db =>
            await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen))
            .GetAwaiter().GetResult();

        _useSecondaryPrice = cashRegister?.IsSecPrice ?? false;
    }

    private void LoadOrderDetails(Order order)
    {
        OrderItems.Clear();
        if (order.Items != null)
        {
            foreach (var item in order.Items)
            {
                OrderItems.Add(item);
            }
        }
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
        {
            // Create a copy of the article with the display price set appropriately
            var displayArticle = new Article
            {
                Id = article.Id,
                Name = article.Name,
                PriPrice = GetDisplayPrice(article), // Use the appropriate price for display
                SecPrice = article.SecPrice,
                Category = article.Category,
                IsDeleted = article.IsDeleted,
                IsSideDish = article.IsSideDish,
                SideDishes = article.SideDishes
            };
            ListArticles.Add(displayArticle);
        }
    }

    private float GetDisplayPrice(Article article)
    {
        if (_useSecondaryPrice && article.SecPrice > 0)
        {
            return article.SecPrice;
        }
        return article.PriPrice;
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
            var filteredArticles = ListArticles
                .Where(a => a.Name != null && a.Name.ToLower().Contains(searchText))
                .ToList();

            listArticleDataGrid.ItemsSource = filteredArticles;
        }

        // Select first item after filtering
        if (listArticleDataGrid.ItemsSource is System.Collections.IList items && items.Count > 0)
        {
            listArticleDataGrid.SelectedIndex = 1;
            listArticleDataGrid.ScrollToRowIndex(0);
        }

        // Maintain search bar focus
        FocusSearchBarAsync();
    }

    private void OnSearchBarSearchButtonPressed(object sender, EventArgs e)
    {
        AddSelectedArticleToOrder();
    }

    // Method for search bar focus management
    private void FocusSearchBarAsync()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(50);
            searchBar.Focus();
        });
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

        // Determine which price to use based on cash register setting and availability
        decimal priceToUse;
        if (_useSecondaryPrice && article.SecPrice > 0)
        {
            priceToUse = (decimal)article.SecPrice;
        }
        else
        {
            priceToUse = (decimal)article.PriPrice;
        }

        if (existingOrderItem != null)
        {
            // If it exists, increase the quantity
            existingOrderItem.Quantity++;
        }
        else
        {
            // Get the original article data for saving (not the display version)
            var originalArticle = AppDbContext.ExecuteSafeAsync(async db =>
                await db.Articles.FindAsync(article.Id))
                .GetAwaiter().GetResult();

            // Create new order item with appropriate price
            var newOrderItem = new OrderItem
            {
                Article = originalArticle,
                Quantity = 1,
                UnitPrice = priceToUse
            };

            OrderItems.Add(newOrderItem);
        }

        UpdateOrderTotals();
    }


    // Toggle focus between search bar and order items table
    public void ToggleTableFocus()
    {
        if (searchBar.IsFocused)
        {
            // Switch to order items table
            if (OrderItems.Count > 0)
            {
                orderItemsDataGrid.SelectedIndex = 1;
                orderItemsDataGrid.Focus();
                orderItemsDataGrid.MoveCurrentCellTo(new Syncfusion.Maui.GridCommon.ScrollAxis.RowColumnIndex(1, 1));
                orderItemsDataGrid.ScrollToRowIndex(1);

                // Activate keyboard navigation without simulating Tab
                if (orderItemsDataGrid.SelectionController is CustomRowSelectionController controller)
                {
                    controller.ActivateKeyboardNavigation();
                }
            }
        }
        else
        {
            // Switch back to search bar
            FocusSearchBarAsync();
        }
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();

        this.HandlerChanged += OnHandlerChanged;
        orderItemsDataGrid.SelectionController = new CustomRowSelectionController(orderItemsDataGrid, this);
        listArticleDataGrid.SelectionController = new CustomArticleSelectionController(listArticleDataGrid, this);

        OnHandlerChanged(this, EventArgs.Empty);

        searchBar.Text = string.Empty;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(100); // Give MAUI/WinUI time to fully render the searchBar

#if WINDOWS
            var autoSuggestBox = searchBar.Handler?.PlatformView as Microsoft.UI.Xaml.Controls.AutoSuggestBox;
            if (autoSuggestBox != null)
            {
                autoSuggestBox.KeyDown -= SearchBarPlatformView_KeyDown;
                autoSuggestBox.KeyDown += SearchBarPlatformView_KeyDown;
                autoSuggestBox.KeyUp -= SearchBarPlatformView_KeyUp;
                autoSuggestBox.KeyUp += SearchBarPlatformView_KeyUp;
            }
#endif

            searchBar.Focus();
        });

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
            case Windows.System.VirtualKey.Escape:
                OnExitSave(this, EventArgs.Empty);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.F2:
                OnPrintReceipt(this, EventArgs.Empty);
                e.Handled = true;
                break;
        }
    }

    private void SearchBarPlatformView_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        //Console.WriteLine($"SearchBar KeyUp: {e.Key}");
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            //Console.WriteLine("Escape pressed in search bar (KeyUp)");
            OnExitSave(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void SearchBarPlatformView_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Down:
                NavigateArticleGrid(1);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Up:
                NavigateArticleGrid(-1);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Enter:
                AddSelectedArticleToOrder();
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Tab:
                ToggleTableFocus();
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Escape:
                OnExitSave(this, EventArgs.Empty);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.F2:
                OnPrintReceipt(this, EventArgs.Empty);
                e.Handled = true;
                break;
        }
    }
#endif


    // Custom selection controller for the order items table
    // This controller handles keyboard navigation and selection in the order items grid
    public class CustomRowSelectionController : DataGridRowSelectionController
    {
        private readonly OrderVisual _parent;
        public CustomRowSelectionController(SfDataGrid dataGrid, OrderVisual parent) : base(dataGrid)
        {
            _parent = parent;
        }

        public void ActivateKeyboardNavigation()
        {
            // Directly activate keyboard navigation without simulating Tab
            // This avoids triggering the Tab key handler that switches focus back to search bar
            var rightArrowArgs = new KeyEventArgs(KeyboardKey.Right) { Handled = false };
            base.ProcessKeyDown(rightArrowArgs, false, false);
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
                    _parent.UpdateOrderTotals();
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
            else if (args.Key == KeyboardKey.Tab)
            {
                _parent.FocusSearchBarAsync();
                args.Handled = true;
            }
            else if (args.Key == KeyboardKey.Escape)
            {
                _parent.OnExitSave(_parent, EventArgs.Empty);
                args.Handled = true;
            }
            else if (args.Key == KeyboardKey.F2)
            {
                _parent.OnPrintReceipt(_parent, EventArgs.Empty);
                args.Handled = true;
            }
            else
            {
                base.ProcessKeyDown(args, isCtrlKeyPressed, isShiftKeyPressed);
            }
        }
    }


    // Custom selection controller for the article grid
    public class CustomArticleSelectionController : DataGridRowSelectionController
    {
        private readonly OrderVisual _parent;

        public CustomArticleSelectionController(SfDataGrid dataGrid, OrderVisual parent) : base(dataGrid)
        {
            _parent = parent;
        }

        protected override void ProcessKeyDown(KeyEventArgs args, bool isCtrlKeyPressed, bool isShiftKeyPressed)
        {
            // Only handle keys when the article grid has direct focus (not when search bar is focused)
            if (_parent.searchBar.IsFocused)
            {
                return;
            }

            if (args.Key == KeyboardKey.Enter)
            {
                int selectedIndex = DataGrid != null ? DataGrid.SelectedIndex : -1;
                if (selectedIndex >= 0 && DataGrid != null)
                {
                    var currentItems = DataGrid.ItemsSource as System.Collections.IList ?? _parent.ListArticles;
                    if (selectedIndex < currentItems.Count && currentItems[selectedIndex] is Article selectedArticle)
                    {
                        _parent.AddArticleToOrder(selectedArticle);
                        args.Handled = true;
                    }
                }
            }
            else if (args.Key == KeyboardKey.Space)
            {
                _parent.ToggleTableFocus();
                args.Handled = true;
            }
            else if (args.Key == KeyboardKey.Escape)
            {
                _parent.OnExitSave(_parent, EventArgs.Empty);
                args.Handled = true;
            }
            else if (args.Key == KeyboardKey.F2)
            {
                _parent.OnPrintReceipt(_parent, EventArgs.Empty);
                args.Handled = true;
            }
            else
            {
                base.ProcessKeyDown(args, isCtrlKeyPressed, isShiftKeyPressed);
            }
        }
    }


    // Button action methods
    // when "+" is clicked, increase the quantity of the selected item
    private void OnIncreaseQuantityClicked(object sender, EventArgs e)
    {
        var selectedItemIndex = orderItemsDataGrid.SelectedIndex;

        if (selectedItemIndex >= 0 && selectedItemIndex <= OrderItems.Count)
        {
            var selectedItem = OrderItems[selectedItemIndex - 1];
            selectedItem.Quantity++;
            UpdateOrderTotals();
        }
    }

    // when "-" is clicked, decrease the quantity of the selected item
    private void OnDecreaseQuantityClicked(object sender, EventArgs e)
    {
        var selectedItemIndex = orderItemsDataGrid.SelectedIndex;

        if (selectedItemIndex >= 0 && selectedItemIndex <= OrderItems.Count)
        {
            var selectedItem = OrderItems[selectedItemIndex - 1];
            if (selectedItem.Quantity > 1)
            {
                selectedItem.Quantity--;
                UpdateOrderTotals();
            }
            else
            {
                OrderItems.Remove(selectedItem);
            }
        }
    }

    // when "delete" is clicked, remove the selected item from the order
    private void OnRemoveItemClicked(object sender, EventArgs e)
    {
        var selectedItemIndex = orderItemsDataGrid.SelectedIndex;

        if (selectedItemIndex >= 0 && selectedItemIndex <= OrderItems.Count)
        {
            var selectedItem = OrderItems[selectedItemIndex - 1];
            OrderItems.Remove(selectedItem);
        }
    }

    // when "enter" is clicked, open the quantity editor for the selected item
    public void EditOrderItemQuantity(OrderItem item)
    {
        int rowIndex = OrderItems.IndexOf(item) + 1; // +1 because the first row is empty in the DataGrid
        int quantityColumnIndex = 0;
        if (rowIndex >= 0)
            orderItemsDataGrid.BeginEdit(rowIndex, quantityColumnIndex);
    }


    private async void OnExitSave(object sender, EventArgs e)
    {
        if (OrderItems.Any(item => item.Quantity < 0))
        {
            _ = DisplayAlert("Cantidad invalida", "No se puede guardar una orden con cantidades negativas.", "OK");
            return;
        }

        if (!OrderItems.Any())
        {
            var result = await DisplayAlert("Orden vacia",
                "¿Desea cerrar esta orden sin articulos?",
                "Si, cerrar",
                "No, cancelar");

            if (!result) return;
        }

        if (_currentOrder != null)
        {
            _currentOrder.Items = OrderItems.ToList();
            _currentOrder.Discount = _discountAmount;
            _currentOrder.IsDuePaid = dueToPayCheckBox.IsChecked;

            try
            {
                using var db = new AppDbContext();

                if (_currentOrder.Id == 0)
                    db.Orders.Add(_currentOrder);
                else
                    db.Orders.Update(_currentOrder);

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo guardar la orden: {ex.Message}", "OK");
                return;
            }
        }

        CloseThisWindow();
    }


    private void CloseThisWindow()
    {
        var app = Microsoft.Maui.Controls.Application.Current;
        if (app != null)
        {
            foreach (var window in app.Windows)
            {
                if (window.Page == this)
                {
                    app.CloseWindow(window);
                    FocusOrderSectionSearchBar();
                    break;
                }
            }
        }
    }

    private void FocusOrderSectionSearchBar()
    {
        var app = Microsoft.Maui.Controls.Application.Current;
        if (app != null)
        {
            foreach (var window in app.Windows)
            {
                
                // Check if the page is directly OrderSectionMainVisual
                if (window.Page is OrderSectionMainVisual orderSectionPage)
                {
                    orderSectionPage.FocusSearchBar();
                    Console.WriteLine("Focused search bar in OrderSectionMainVisual");
                    break;
                }

                // Check if it's wrapped in a NavigationPage
                else if (window.Page is NavigationPage navPage && navPage.CurrentPage is OrderSectionMainVisual orderSectionMainPage)
                {
                    orderSectionMainPage.FocusSearchBar();
                    Console.WriteLine("Focused search bar in OrderSectionMainVisual (via NavigationPage)");
                    break;
                }
            }
        }
    }

    private async void OnPrintReceipt(object sender, EventArgs e)
    {

        if (_currentOrder != null)
        {
            _currentOrder.Items = OrderItems.ToList();
            _currentOrder.Discount = _discountAmount;
            _currentOrder.IsDuePaid = dueToPayCheckBox.IsChecked;

            //this need to be wrapped in a try catch block to handle any potential database errors
            using var db = new AppDbContext();
            db.Orders.Update(_currentOrder);
            db.SaveChanges();
        }

        Console.WriteLine("Print receipt clicked");
        if (_currentOrder != null)
        {
            try
            {
                using var db = new AppDbContext();
                _currentOrder.IsBillRequested = true;
                db.Orders.Update(_currentOrder);
                await db.SaveChangesAsync();

                if (ListOrderVisual.Instance != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ListOrderVisual.Instance.ReloadTM();
                    });
                }

                //CloseThisWindow();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo actualizar la orden: {ex.Message}", "OK");
            }
        }

        string templateText = File.ReadAllText(@"C:\Codes\github\felix-1-proyect\felix1\ReceiptTemplates\OrderTemplate.txt");
        var template = Template.Parse(templateText);
        var scribanModel = new { order = _currentOrder };
        string text = template.Render(scribanModel, member => member.Name);


#if WINDOWS

                try
                {
                    PrintDocument pd = new PrintDocument();
                    //pd.PrinterSettings.PrinterName = "Star SP500 Cutter"; // or whatever name shows in Windows, but it should take the default one
                    pd.PrintPage += (sender, e) =>
                    {
                        System.Drawing.Font font = new System.Drawing.Font("Consolas", 10); // Monospaced font recommended for POS printers
                        e.Graphics.DrawString(text, font, Brushes.Black, new System.Drawing.PointF(10, 10));
                    };
                    
                    pd.Print();
                    Console.WriteLine("Printed successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Print failed: " + ex.Message);
                }
#else
        Console.WriteLine("Printing is only supported on Windows for now.");
        await DisplayAlert("Error", $"No se pudo imprimir el recibo: La funcionalidad de impresión no está disponible en esta plataforma (solo Windows).", "OK");
#endif

        OnExitSave(sender, e); // Save the order after printing and close the window
    }

    // Navigation methods for article grid
    public void NavigateArticleGrid(int direction)
    {
        var currentItems = listArticleDataGrid.ItemsSource as System.Collections.IList ?? ListArticles;

        if (currentItems.Count == 0) return;

        int currentIndex = listArticleDataGrid.SelectedIndex;
        int newIndex = currentIndex + direction;

        // Clamp to valid range
        newIndex = Math.Max(1, Math.Min(newIndex, currentItems.Count));

        listArticleDataGrid.SelectedIndex = newIndex;
        listArticleDataGrid.ScrollToRowIndex(newIndex);

        FocusSearchBarAsync();
    }

    public void AddSelectedArticleToOrder()
    {
        var currentItems = listArticleDataGrid.ItemsSource as System.Collections.IList;
        if (currentItems == null) return;

        int selectedIndex = listArticleDataGrid.SelectedIndex - 1; // Adjust for header row

        if (selectedIndex >= 0 && selectedIndex <= currentItems.Count)
        {
            if (currentItems[selectedIndex] is Article selectedArticle)
            {
                AddArticleToOrder(selectedArticle);
                FocusSearchBarAsync();
            }
        }
    }

    // Order calculation methods
    private void UpdateOrderTotals()
    {
        decimal subtotal = CalculateSubtotal();
        decimal tax = CalculateTax(subtotal);
        decimal total = subtotal + tax - _discountAmount;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            subtotalLabel.Text = subtotal.ToString("C2");
            taxLabel.Text = tax.ToString("C2");
            totalLabel.Text = total.ToString("C2");
            //discountEntry.Text = _discountAmount.ToString("C2");
        });
    }

    private decimal CalculateSubtotal()
    {
        return OrderItems.Sum(item => item.TotalPrice);
    }

    private decimal CalculateTax(decimal subtotal)
    {
        return subtotal * TAX_RATE;
    }

    private void OnDiscountChanged(object sender, TextChangedEventArgs e)
    {
        if (decimal.TryParse(e.NewTextValue, out decimal discount))
        {
            _discountAmount = Math.Max(0, discount); // Ensure discount is not negative
        }
        else
        {
            _discountAmount = 0;
        }

        UpdateOrderTotals();
    }

}
