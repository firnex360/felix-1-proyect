namespace felix1.AdminSection;
using Microsoft.Maui.Storage; // For Preferences
public partial class Configuration : ContentView
{
    public Configuration()
    {
        InitializeComponent();
        LoadSavedData();
    }

    private void LoadSavedData()
    {
        // Load all saved values (fall back to defaults if not found)
        txtCompany.Text = Preferences.Get("CompanyName", "FELIX I");
        txtAddress.Text = Preferences.Get("CompanyAddress", "AV. HISPANOAMERICA");
        txtPhone.Value = double.Parse(Preferences.Get("CompanyPhone", "8095828134"));
        txtEmail.Text = Preferences.Get("CompanyEmail", "");
        txtRNC.Value = double.Parse(Preferences.Get("CompanyRNC", "130622914"));
        txtTax.Value = double.Parse(Preferences.Get("TaxRate", "18"));
        txtDeliveryTax.Value = double.Parse(Preferences.Get("DeliveryTaxRate", "10"));
        txtWaiterTax.Value = double.Parse(Preferences.Get("WaiterTaxRate", "5"));
        txtComment.Text = Preferences.Get("InvoiceComment", "GRACIAS POR PREFERIRNOS");
    }
    
     private async void btnSave_Clicked(object sender, EventArgs e)
    {
        try 
        {
            // Save all values
            Preferences.Set("CompanyName", txtCompany.Text);
            Preferences.Set("CompanyAddress", txtAddress.Text);
            Preferences.Set("CompanyPhone", txtPhone.Value.ToString());
            Preferences.Set("CompanyEmail", txtEmail.Text);
            Preferences.Set("CompanyRNC", txtRNC.Value.ToString());
            Preferences.Set("TaxRate", txtTax.Value.ToString());
            Preferences.Set("DeliveryTaxRate", txtDeliveryTax.Value.ToString());
            Preferences.Set("WaiterTaxRate", txtWaiterTax.Value.ToString());
            Preferences.Set("InvoiceComment", txtComment.Text);

            await Application.Current.MainPage.DisplayAlert("Éxito", "Configuración guardada correctamente", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo guardar: {ex.Message}", "OK");
        }
    }
}