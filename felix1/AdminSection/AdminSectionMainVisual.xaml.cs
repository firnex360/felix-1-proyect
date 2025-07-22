using felix1.AdminSection;
using Syncfusion.Maui.Buttons;

namespace felix1;

public partial class AdminSectionMainVisual : ContentPage
{
    public AdminSectionMainVisual()
    {
        InitializeComponent();
        SetButtonChecked(btnShowArticle);
        RightPanel.Content = new ListArticleVisual();
        #if WINDOWS
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window != null)
        {
            WindowUtils.MaximizeWindow(window);
        }
        #endif
    }

    public void SetRightPanelContent(ContentView content)
    {
        RightPanel.Content = content;
    }

    public ContentView RightPanelView => RightPanel;

    private void SetButtonChecked(SfButton checkedButton)
    {
        btnShowArticle.IsChecked = checkedButton == btnShowArticle;
        btnShowUser.IsChecked = checkedButton == btnShowUser;
        btnShowCuadre.IsChecked = checkedButton == btnShowCuadre;
        btnShowConfiguration.IsChecked = checkedButton == btnShowConfiguration;
    }

    private async void OnShowArticle(object sender, EventArgs e)
    {
        try
        {
            SetButtonChecked(btnShowArticle);
            RightPanel.Content = new ListArticleVisual();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar la seccion de articulos: {ex.Message}", "OK");
        }
    }

    private async void OnShowUser(object sender, EventArgs e)
    {
        try
        {
            SetButtonChecked(btnShowUser);
            RightPanel.Content = new ListUserVisual();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar la seccion de usuarios: {ex.Message}", "OK");
        }
    }

    private async void OnShowCuadre(object sender, EventArgs e)
    {
        try
        {
            SetButtonChecked(btnShowCuadre);
            RightPanel.Content = new ListCashRegisterVisual(); 
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar el historial de cuadre: {ex.Message}", "OK");
        }
    }

    private async void OnShowConfiguration(object sender, EventArgs e)
    {
        try
        {
            SetButtonChecked(btnShowConfiguration);
            RightPanel.Content = new Configuration();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cargar la configuracion: {ex.Message}", "OK");
        }
    }

    private async void OnExitButtonClicked(object sender, EventArgs e)
    {
        try
        {
            if (Application.Current != null)
            {
                Application.Current.MainPage = new NavigationPage(new LoginPage());
            }
            else
            {
                await DisplayAlert("Error", "No se pudo cerrar la sesión: Application.Current es null.", "OK");
            }
        }
        /*{
            Application.Current!.MainPage = new NavigationPage(new LoginPage());
        }*/
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cerrar la sesion: {ex.Message}", "OK");
        }
    }
}