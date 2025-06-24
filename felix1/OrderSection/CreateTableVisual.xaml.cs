using felix1.Data;
using felix1.Logic;

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
		using var db = new AppDbContext();
		lblMesero.Text = $"Mesero: {_mesero.Name}";
		lblCajero.Text = $"ID: { db.CashRegisters.FirstOrDefault(c => c.IsOpen)}";
		lblFecha.Text = $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}";
	}

	private async void OnCreateClicked(object sender, EventArgs e)
	{


		using var db = new AppDbContext();
		var user = db.Users.Find(AppSession.CurrentUser.Id);


		var waiter = db.Users.Find(_mesero.Id);
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
		db.Tables.Add(table);
		db.SaveChanges();




		var order = new Order
		{
			Number = int.Parse(txtOrderNumber.Value.ToString() ?? "0"),
			Date = DateTime.Now,
			Waiter = waiter,
			Table = table,
			Items = null, //temp
			CashRegister = db.CashRegisters.FirstOrDefault(c => c.IsOpen), //WOULD BE THE ONE WE'RE WORKING WITH, CHECK
			Charge = false
		};
		db.Orders.Add(order);
		db.SaveChanges();

		ListOrderVisual.Instance?.ReloadTables();

		CloseThisWindow();
	}
	

    private void CloseThisWindow()
    {
        foreach (var window in Application.Current.Windows)
        {
            if (window.Page == this)
            {
                Application.Current.CloseWindow(window);
                break;
            }
        }
    }

}