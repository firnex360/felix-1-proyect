namespace felix1.AdminSection;
using Microsoft.Maui.Storage; // For Preferences
using System.Drawing.Printing; // For printer access
public partial class Configuration : ContentView
{
    public Configuration()
    {
        InitializeComponent();
        LoadSavedData();
        LoadAvailablePrinters();
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
        
        // Load selected printer
        var savedPrinter = Preferences.Get("SelectedPrinter", "");
        if (!string.IsNullOrEmpty(savedPrinter))
        {
            pickerPrinter.SelectedItem = savedPrinter;
        }
    }

    private void LoadAvailablePrinters()
    {
        try
        {
#if WINDOWS
            pickerPrinter.Items.Clear();
            
            // Get all installed printers
            foreach (string printerName in PrinterSettings.InstalledPrinters)
            {
                pickerPrinter.Items.Add(printerName);
            }
            
            // If no printers found, add a message
            if (pickerPrinter.Items.Count == 0)
            {
                pickerPrinter.Items.Add("No hay impresoras disponibles");
            }
#else
            // For other platforms, show a message
            pickerPrinter.Items.Clear();
            pickerPrinter.Items.Add("Solo disponible en Windows");
#endif
        }
        catch (Exception ex)
        {
            pickerPrinter.Items.Clear();
            pickerPrinter.Items.Add($"Error: {ex.Message}");
        }
    }

    private async void btnRefreshPrinters_Clicked(object sender, EventArgs e)
    {
        try
        {
            LoadAvailablePrinters();
            await Application.Current!.MainPage!.DisplayAlert("Éxito", "Lista de impresoras actualizada", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", $"No se pudo actualizar: {ex.Message}", "OK");
        }
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
            
            // Save selected printer
            if (pickerPrinter.SelectedItem != null)
            {
                Preferences.Set("SelectedPrinter", pickerPrinter.SelectedItem.ToString());
            }

            await Application.Current!.MainPage!.DisplayAlert("Éxito", "Configuración guardada correctamente", "OK");
        }
        catch (Exception ex)
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", $"No se pudo guardar: {ex.Message}", "OK");
        }
    }
}