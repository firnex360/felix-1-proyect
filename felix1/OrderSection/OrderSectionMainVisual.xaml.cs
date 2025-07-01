using felix1.Data;
using felix1.Logic;

namespace felix1.OrderSection;

public partial class OrderSectionMainVisual : ContentPage
{
    private CashRegister _cashRegister;

    [Obsolete]
    public OrderSectionMainVisual(CashRegister cashRegister)
    {
        InitializeComponent();
        _cashRegister = cashRegister;
        DisplayCashRegisterInfo();
        RightPanel.Content = new ListOrderVisual();
#if WINDOWS
            WindowUtils.MaximizeWindow(Application.Current.Windows.FirstOrDefault());
#endif
    }

    private void DisplayCashRegisterInfo()
    {
        lblCashRegisterInfo.Text = $"Caja #{_cashRegister.Number} " /*+
                                 $"Abierta por: {_cashRegister.Cashier?.Name}\n" +
                                 $"Hora de apertura: {_cashRegister.TimeStarted:dd/MM/yyyy HH:mm}\n"*/;
    }

    private async void OnCloseRegister(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmación",
            $"¿Cerrar la caja?",
            "Sí",
            "No");

        if (!confirm)
            return;

        await AppDbContext.ExecuteSafeAsync(async db =>
        {
            var user = await db.Users.FindAsync(AppSession.CurrentUser.Id);
            var register = await db.CashRegisters.FindAsync(_cashRegister.Id);

            if (register != null)
            {
                register.Cashier = user;
                register.IsOpen = false;
                register.TimeFinish = DateTime.Now;
                db.CashRegisters.Update(register);
                await db.SaveChangesAsync();
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                Navigation.PopAsync();
                var loginPage = new LoginPage();
                Application.Current.MainPage = new NavigationPage(loginPage);
            });
        });
    }

    private async void OnCloseSesion(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmación",
            "¿Estás seguro que deseas cerrar sesión?",
            "Sí",
            "No");

        if (!confirm)
            return;

        AppSession.CurrentUser = null;

        var loginPage = new LoginPage();
        Application.Current.MainPage = new NavigationPage(loginPage);
    }
}