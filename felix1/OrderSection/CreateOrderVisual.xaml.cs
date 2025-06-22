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

		//timePicker.Time = now.TimeOfDay;
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

            var newCashRegister = new CashRegister
			{
				Number = (int)(txtNumber.Text != null ? float.Parse(txtNumber.Text) : 0f),
				InitialMoney = txtInitialMoney.Text != null ? float.Parse(txtInitialMoney.Text) : 0f,
				TimeStarted = datePicker.Date,
				IsSecPrice = cbxSecondaryPrice.IsChecked,
				IsOpen = true,
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