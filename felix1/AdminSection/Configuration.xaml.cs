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
        txtCompany.Text = Preferences.Get("CompanyName", "Sin nombre");
        txtAddress.Text = Preferences.Get("CompanyAddress", "Sin dirección");
        
        // Use TryParse to handle invalid string formats safely
        var phoneStr = Preferences.Get("CompanyPhone", "0");
        txtPhone.Value = double.TryParse(phoneStr, out double phone) ? phone : 0;
        
        txtEmail.Text = Preferences.Get("CompanyEmail", "");
        
        var rncStr = Preferences.Get("CompanyRNC", "0");
        txtRNC.Value = double.TryParse(rncStr, out double rnc) ? rnc : 0;
        
        var taxStr = Preferences.Get("TaxRate", "18");
        txtTax.Value = double.TryParse(taxStr, out double tax) ? tax : 18;
        
        var deliveryTaxStr = Preferences.Get("DeliveryTaxRate", "0");
        txtDeliveryTax.Value = double.TryParse(deliveryTaxStr, out double deliveryTax) ? deliveryTax : 0;
        
        var waiterTaxStr = Preferences.Get("WaiterTaxRate", "10");
        txtWaiterTax.Value = double.TryParse(waiterTaxStr, out double waiterTax) ? waiterTax : 10;
        
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

            await Application.Current!.MainPage!.DisplayAlert("Éxito", "Configuración guardada correctamente", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", $"No se pudo guardar: {ex.Message}", "OK");
        }
    }
}