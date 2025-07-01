using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using felix1.Data;
using felix1.Logic;

namespace felix1.OrderSection;

public partial class CreateCashRegisterVisual : ContentPage
{
    private CashRegister? existingOpenRegister = null;

    public CreateCashRegisterVisual()
    {
        InitializeComponent();
        LoadCashRegisterStatus();
    }

    private async void LoadCashRegisterStatus()
    {
        existingOpenRegister = await AppDbContext.ExecuteSafeAsync(async db =>
            await db.CashRegisters
                .Include(c => c.Cashier)
                .FirstOrDefaultAsync(c => c.IsOpen));

        if (existingOpenRegister != null)
        {
            if (existingOpenRegister.Cashier?.Id == AppSession.CurrentUser.Id)
            {
                var response = await ShowNonClosableAlert("Caja Abierta",
                    $"Ya hay una caja abierta por ti. ¿Desea continuar usando esta caja?",
                    "Sí, deseo continuar",
                    "No, cerrar y crear nueva");

                if (response)
                {
                    // User wants to continue with existing register
                    await OpenOrderSectionMain(existingOpenRegister);
                    return;
                }
                else
                {
                    // User wants to close the register
                    await AppDbContext.ExecuteSafeAsync(async db =>
                    {
                        existingOpenRegister.IsOpen = false;
                        existingOpenRegister.TimeFinish = DateTime.Now;
                        db.CashRegisters.Update(existingOpenRegister);
                        await db.SaveChangesAsync();
                    });

                    ShowCreateNewRegisterView();
                    return;
                }
            }
            else
            {
                var response = await ShowNonClosableAlert("Caja Abierta",
                    $"Hay una caja abierta por {existingOpenRegister.Cashier?.Name}. ¿Desea continuar usando esta caja?",
                    "Sí, deseo continuar",
                    "No, cerrar y crear nueva");

                if (response)
                {
                    await OpenOrderSectionMain(existingOpenRegister);
                    return;
                }
                else
                {
                    await AppDbContext.ExecuteSafeAsync(async db =>
                    {
                        existingOpenRegister.IsOpen = false;
                        existingOpenRegister.TimeFinish = DateTime.Now;
                        db.CashRegisters.Update(existingOpenRegister);
                        await db.SaveChangesAsync();
                    });

                    ShowCreateNewRegisterView();
                    return;
                }
            }
        }

        ShowCreateNewRegisterView();
    }

    private async Task<bool> ShowNonClosableAlert(string title, string message, string accept, string cancel)
    {
        bool? result = null;

        while (result == null)
        {
            result = await DisplayAlert(title, message, accept, cancel);
        }

        return result.Value;
    }

    private void ShowCreateNewRegisterView()
    {
        lblCashRegisterStatus.Text = "No hay cajas abiertas";
        lblActionPrompt.Text = "Por favor, abrir una caja antes de empezar";
        btnCrearCajaNueva.IsVisible = false;
        btnCrearCaja.IsVisible = true;
    }

    private async void OnSaveCashRegister(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtInitialMoney.Value?.ToString()))
        {
            await DisplayAlert("Error", "El campo 'Dinero Inicial' es obligatorio.", "OK");
            return;
        }

        bool hasOpenRegister = await AppDbContext.ExecuteSafeAsync(async db =>
            await db.CashRegisters.AnyAsync(c => c.IsOpen));

        if (hasOpenRegister)
        {
            await DisplayAlert("Error", "Ya hay una caja abierta en el sistema.", "OK");
            return;
        }

        await AppDbContext.ExecuteSafeAsync(async db =>
        {
            var user = await db.Users.FindAsync(AppSession.CurrentUser.Id);

            var newCashRegister = new CashRegister
            {
                Number = 1,
                InitialMoney = float.Parse(txtInitialMoney.Value.ToString() ?? "0"),
                TimeStarted = DateTime.Now,
                IsSecPrice = cbxSecondaryPrice.IsChecked,
                IsOpen = true,
                Cashier = user,
            };

            db.CashRegisters.Add(newCashRegister);
            await db.SaveChangesAsync();

            await OpenOrderSectionMain(newCashRegister);
        });
    }

    private async Task OpenOrderSectionMain(CashRegister cashRegister)
    {
        var orderSectionMain = new OrderSectionMainVisual(cashRegister);
        await Navigation.PushAsync(orderSectionMain);
        Navigation.RemovePage(this);
    }

    private void OnShowCreateForm(object sender, EventArgs e)
    {
        txtInitialMoney.Value = null;
        cbxSecondaryPrice.IsChecked = false;
        ShowCreateNewRegisterView();
    }
}