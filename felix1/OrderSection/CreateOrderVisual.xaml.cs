using System.Text.RegularExpressions;
using felix1.Data;
using felix1.Logic;

namespace felix1.OrderSection;

public partial class CreateOrderVisual : ContentPage
{

	private CashRegister? editingCashRegister = null;
    public CreateOrderVisual()
    {
        InitializeComponent();
        
        LoadCashRegisterStatus();

		//timePicker.Time = now.TimeOfDay;
    }

    //called when the page is loaded
    private void LoadCashRegisterStatus()
    {
        using var db = new AppDbContext();
        var openRegisters = db.CashRegisters
                            .Where(c => c.IsOpen)
                            .ToList();

        if (openRegisters.Count > 0)
        {
            lblCashRegisterStatus.Text = $"Existen {openRegisters.Count} caja(s) abierta(s) en el sistema";
            lblActionPrompt.Text = "Puede seleccionar una caja existente o crear una nueva";

            pickerOpenRegisters.ItemsSource = openRegisters
                .Select(r => $"Caja #{r.Number} - {r.TimeStarted:dd/MM/yyyy}")
                .ToList();

            pickerOpenRegisters.IsVisible = true;
            ShowForm(false);

            btnCrearCajaNueva.IsVisible = true;
            btnCrearCaja.IsVisible = false;
        }
        else
        {
            lblCashRegisterStatus.Text = "No hay cajas abiertas";
            lblActionPrompt.Text = "Por favor, abrir una caja antes de empezar";

            pickerOpenRegisters.IsVisible = false;
            ShowForm(true);
            
            btnCrearCajaNueva.IsVisible = false;
            btnCrearCaja.IsVisible = true;

        }
    }

    private void OnShowCreateForm(object sender, EventArgs e)
    {
        pickerOpenRegisters.IsVisible = false;
        ShowForm(true);
        lblCashRegisterStatus.Text = "Crear nueva caja";
        lblActionPrompt.Text = string.Empty;


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
        // VALIDATION

        if (string.IsNullOrWhiteSpace(txtInitialMoney.Text))
        {
            await DisplayAlert("Error", "El campo 'Dinero Inicial' es obligatorio.", "OK");
            return;
        }




        //POPUP CONFIRMATION
        bool confirm = await DisplayAlert(
            "Confirmación",
            editingCashRegister == null ? "¿Crear esta caja?" : "¿Actualizar este artículo?",
            "Sí",
            "No");

        if (!confirm)
            return;

        using var db = new AppDbContext();

        
        if (editingCashRegister == null)
        {
			var now = DateTime.Now;
			datePicker.Date = now.Date;//verify
            var user = db.Users.Find(AppSession.CurrentUser.Id);

            var newCashRegister = new CashRegister
            {
                Number = (int)(txtNumber.Text != null ? float.Parse(txtNumber.Text) : 0f),
                InitialMoney = txtInitialMoney.Text != null ? float.Parse(txtInitialMoney.Text) : 0f,
                TimeStarted = datePicker.Date,
                IsSecPrice = cbxSecondaryPrice.IsChecked,
                IsOpen = true,
                Cashier = user, 
			};

            db.CashRegisters.Add(newCashRegister);

        }
        /*else
        {
            // UPDATE EXISTING
            var cashRegister = db.CashRegisters
                .FirstOrDefault(a => a.Id == editingCashRegister.Id);

            if (cashRegister != null)
            {
                cashRegister.Number = txtName.Text;
                cashRegister.PriPrice = float.TryParse(txtPrice.Text, out var pri) ? pri : 0f;
                cashRegister.SecPrice = float.TryParse(txtSecondaryPrice.Text, out var sec) ? sec : 0f;
                cashRegister.Category = parsed ? categoryEnum : ArticleCategory.Other;
                cashRegister.IsSideDish = txtSideDish.IsChecked;

                db.CashRegisters.Update(cashRegister);
            }
        }*/

        db.SaveChanges();
        //ListArticleVisual.Instance?.ReloadArticles(); // REFRESH THE LIST
        await DisplayAlert("Éxito", "Se ha creado una caja exitosamente.", "OK");

    }




}