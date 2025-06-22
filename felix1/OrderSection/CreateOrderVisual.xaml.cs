using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using felix1.Data;
using felix1.Logic;

namespace felix1.OrderSection;

public partial class CreateOrderVisual : ContentPage
{
    private CashRegister? existingOpenRegister = null;

    public CreateOrderVisual()
    {
        InitializeComponent();
        LoadCashRegisterStatus();
    }

    private async void LoadCashRegisterStatus()
    {
        using var db = new AppDbContext();
        existingOpenRegister = db.CashRegisters
                            .Include(c => c.Cashier)
                            .FirstOrDefault(c => c.IsOpen);

        if (existingOpenRegister != null)
        {
            if (existingOpenRegister.Cashier?.Id == AppSession.CurrentUser.Id)
            {
                var response = await DisplayAlert("Caja Abierta",
                    $"Ya hay una caja abierta (Caja #{existingOpenRegister.Number}) por ti. ¿Desea continuar usando esta caja?",
                    "Sí",
                    "No");

                if (response)
                {
                    // User wants to continue with existing register
                    await OpenOrderSectionMain(existingOpenRegister);
                    return;
                }
                else
                {
                    // User wants to close the register
                    existingOpenRegister.IsOpen = false;
                    existingOpenRegister.TimeFinish = DateTime.Now;
                    db.CashRegisters.Update(existingOpenRegister);
                    db.SaveChanges();
                    existingOpenRegister = null;
                }
            }
            else
            {
                var close = await DisplayAlert("Caja Abierta",
                    $"Hay una caja abierta (Caja #{existingOpenRegister.Number}) por {existingOpenRegister.Cashier?.Name}. Solo puede cerrarla.",
                    "Cerrar caja",
                    "Cancelar");

                if (close)
                {
                    existingOpenRegister.IsOpen = false;
                    existingOpenRegister.TimeFinish = DateTime.Now;
                    db.CashRegisters.Update(existingOpenRegister);
                    db.SaveChanges();
                    existingOpenRegister = null;
                }
                else
                {
                    await Navigation.PopAsync();
                    return;
                }
            }
        }

        ShowCreateNewRegisterView();
    }

    private void ShowCreateNewRegisterView()
    {
        lblCashRegisterStatus.Text = "No hay cajas abiertas";
        lblActionPrompt.Text = "Por favor, abrir una caja antes de empezar";

        ShowForm(true);

        btnCrearCajaNueva.IsVisible = false;
        btnCrearCaja.IsVisible = true;
    }

    private void ShowForm(bool show)
    {
        lblNumber.IsVisible = show;
        txtNumber.IsVisible = show;
        lblInitialMoney.IsVisible = show;
        txtInitialMoney.IsVisible = show;
        lbldatePicker.IsVisible = show;
        datePicker.IsVisible = show;
        lblSecondaryPrice.IsVisible = show;
        cbxSecondaryPrice.IsVisible = show;
    }

    private void OnNumericEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        string regex = e.NewTextValue;
        if (String.IsNullOrEmpty(regex))
            return;

        if (!Regex.Match(regex, "^[0-9]+$").Success)
        {
            var entry = sender as Entry;
            entry.Text = (string.IsNullOrEmpty(e.OldTextValue)) ?
                    string.Empty : e.OldTextValue;
        }
    }

    private async void OnSaveCashRegister(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtNumber.Text))
        {
            await DisplayAlert("Error", "El campo 'Número de caja' es obligatorio.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtInitialMoney.Text))
        {
            await DisplayAlert("Error", "El campo 'Dinero Inicial' es obligatorio.", "OK");
            return;
        }

        using var checkDb = new AppDbContext();
        if (checkDb.CashRegisters.Any(c => c.IsOpen))
        {
            await DisplayAlert("Error", "Ya hay una caja abierta en el sistema.", "OK");
            return;
        }

        bool confirm = await DisplayAlert(
            "Confirmación",
            "¿Crear esta caja?",
            "Sí",
            "No");

        if (!confirm)
            return;

        using var db = new AppDbContext();
        var user = db.Users.Find(AppSession.CurrentUser.Id);

        var newCashRegister = new CashRegister
        {
            Number = int.Parse(txtNumber.Text),
            InitialMoney = float.Parse(txtInitialMoney.Text),
            TimeStarted = DateTime.Now, 
            IsSecPrice = cbxSecondaryPrice.IsChecked,
            IsOpen = true,
            Cashier = user,
        };

        db.CashRegisters.Add(newCashRegister);
        db.SaveChanges();

        await DisplayAlert("Éxito", "Se ha creado una caja exitosamente.", "OK");
        await OpenOrderSectionMain(newCashRegister);
    }

    private async Task OpenOrderSectionMain(CashRegister cashRegister)
    {
        var orderSectionMain = new OrderSectionMainVisual(cashRegister);
        await Navigation.PushAsync(orderSectionMain);
        Navigation.RemovePage(this);
    }

    private void OnShowCreateForm(object sender, EventArgs e)
    {
        txtNumber.Text = string.Empty;
        txtInitialMoney.Text = string.Empty;
        datePicker.Date = DateTime.Now;
        cbxSecondaryPrice.IsChecked = false;

        ShowCreateNewRegisterView();
    }
}