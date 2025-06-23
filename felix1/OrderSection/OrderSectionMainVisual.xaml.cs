using felix1.Data;
using felix1.Logic;

namespace felix1.OrderSection;

public partial class OrderSectionMainVisual : ContentPage
{
    private CashRegister _cashRegister;

    public OrderSectionMainVisual(CashRegister cashRegister)
    {
        InitializeComponent();
        _cashRegister = cashRegister;
        DisplayCashRegisterInfo();
        //RightPanel.Content = new ListOrderVisual();
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
            "Confirmaci�n",
            $"�Cerrar la caja #{_cashRegister.Number}?",
            "S�",
            "No");

        if (!confirm)
            return;

        using var db = new AppDbContext();
        var register = db.CashRegisters.Find(_cashRegister.Id);
        if (register != null)
        {
            register.IsOpen = false;
            register.TimeFinish = DateTime.Now;
            db.CashRegisters.Update(register);
            db.SaveChanges();
        }

        await Navigation.PopAsync();
    }
}