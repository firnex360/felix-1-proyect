using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;

namespace felix1.OrderSection;

public partial class CreateTableVisual : ContentPage
{
    private User _mesero;
    public CreateTableVisual(User mesero)
    {
        InitializeComponent();
        _mesero = mesero;
        DisplayLabelsInfo();
    }

    private void DisplayLabelsInfo()
    {
        lblMesero.Text = $"Mesero: {_mesero.Name}";

        var cashRegister = AppDbContext.ExecuteSafeAsync(async db =>
            await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen))
            .GetAwaiter().GetResult();

        lblCajero.Text = $"ID: {cashRegister?.Number}";
        lblFecha.Text = $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}";
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        var waiter = await AppDbContext.ExecuteSafeAsync(async db =>
            await db.Users.FindAsync(_mesero.Id));

        if (waiter == null)
        {
            await DisplayAlert("Error", "Mesero no encontrado.", "OK");
            return;
        }

        var table = new Table
        {
            LocalNumber = int.Parse(txtLocalNumber.Value.ToString() ?? "0"),
            GlobalNumber = int.Parse(txtGlobalNumber.Value.ToString() ?? "0"),
            IsTakeOut = false,
            IsBillRequested = false,
            IsPaid = false
        };

        await AppDbContext.ExecuteSafeAsync(async db =>
        {
            db.Tables.Add(table);
            await db.SaveChangesAsync();
        });

        await AppDbContext.ExecuteSafeAsync(async db =>
        {
            var order = new Order
            {
                OrderNumber = int.Parse(txtOrderNumber.Value.ToString() ?? "0"),
                Date = DateTime.Now,
                Waiter = waiter, //comes as a parameter from the list, depending where we clicked
                Table = table, //automatically created before a new order
                Items = null, //temp
                CashRegister = await db.CashRegisters.FirstOrDefaultAsync(c => c.IsOpen),  //we can only work with the open cash register
                IsDuePaid = false,
                IsBillRequested = false
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();
        });

        ListOrderVisual.Instance?.ReloadTM();
        CloseThisWindow();
    }

    private void CloseThisWindow()
    {
        foreach (var window in Application.Current!.Windows)
        {
            if (window.Page == this)
            {
                Application.Current.CloseWindow(window);
                break;
            }
        }
    }
}