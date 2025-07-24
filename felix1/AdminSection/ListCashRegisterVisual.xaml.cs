using felix1.AdminSection;
using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Maui.DataGrid;
using System.Collections.ObjectModel;

namespace felix1;

public partial class ListCashRegisterVisual : ContentView
{
    public static ListCashRegisterVisual? Instance { get; private set; }

    public ObservableCollection<CashRegister> CashRegisters { get; set; } = new();

    public ListCashRegisterVisual()
    {
        InitializeComponent();
        BindingContext = this;
        Instance = this;
        LoadCashRegisters();
    }

    private void LoadCashRegisters()
    {
        var registers = AppDbContext.ExecuteSafeAsync(async db =>
            await db.CashRegisters.ToListAsync()).GetAwaiter().GetResult();

        CashRegisters.Clear();
        foreach (var reg in registers)
        {
            CashRegisters.Add(reg);
        }
    }

    public void RefreshData()
    {
        LoadCashRegisters();
    }

    private void OnDetailsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is CashRegister cashRegister)
        {
            if (Application.Current?.MainPage is NavigationPage navPage &&
                navPage.CurrentPage is AdminSectionMainVisual adminPage)
            {
                adminPage.RightPanelView.Content = new ListPaymentVisual(cashRegister);
            }
        }
    }

    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            dataGrid.ItemsSource = CashRegisters;
        }
        else
        {
            dataGrid.ItemsSource = CashRegisters.Where(cr =>
                cr.Id.ToString().Contains(searchText) ||
                cr.Number.ToString().Contains(searchText) ||
                cr.InitialMoney.ToString().Contains(searchText) ||
                cr.TimeStarted.ToString().Contains(searchText) ||
                (cr.IsOpen ? "abierto" : "cerrado").Contains(searchText)
            ).ToList();
        }
    }
}