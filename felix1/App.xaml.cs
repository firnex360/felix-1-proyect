namespace felix1;
using felix1.Data;
using felix1.Logic;
using felix1.OrderSection;

public partial class App : Application
{
	private Timer _cashRegisterCheckTimer;
	public App()
	{
		Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NNaF5cXmBCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWXleeXVcQmJcUkdwXkBWYUA=");

		InitializeComponent();

		MainPage = new AppShell();
		MainPage = new NavigationPage(new MainPage()); //CHECKING - adding... this, to new page

		_cashRegisterCheckTimer = new Timer(CloseRegisterAutomatically, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));

	}
	private void CloseRegisterAutomatically(object? state)
	{
		try
		{
			using var db = new AppDbContext(); // Usa tu propio DbContext
			var registers = db.CashRegisters.ToList();
			double hoursBeforeClose = double.TryParse(Preferences.Get("AutoCloseHours", "1"), out var h) ? h : 1;


        	var closedRegisters = new List<CashRegister>();
		
			foreach (var register in registers)
			{
				if (register.IsOpen && DateTime.Now - register.TimeStarted >= TimeSpan.FromHours(hoursBeforeClose))
				{
					register.IsOpen = false;
					register.TimeFinish = register.TimeStarted.AddHours(hoursBeforeClose);
					closedRegisters.Add(register);
				}
			}

			db.SaveChanges();
			if (closedRegisters.Any())
			{
				MainThread.BeginInvokeOnMainThread(async () =>
				{
					foreach (var reg in closedRegisters)
					{
						await Current!.MainPage!.DisplayAlert(
							"Caja ha sido cerrada automaticamente",
							$"Caja #{reg.Number} fue cerrada tras {hoursBeforeClose} horas.",
							"OK");
					}
					
                // Si el usuario está en OrderSectionMainVisual, lo sacamos
                if (!(Application.Current!.MainPage is not NavigationPage navPage ||
                    navPage.CurrentPage is not OrderSectionMainVisual))
                {
                    AppSession.CurrentUser = null; // O la lógica que uses para cerrar sesión
                    Application.Current.MainPage = new NavigationPage(new LoginPage());
                }
				});
			}

		}
		catch (Exception ex)
		{
			Console.WriteLine("Error al cerrar cajas automáticamente: " + ex.Message);
		}
	}

}
