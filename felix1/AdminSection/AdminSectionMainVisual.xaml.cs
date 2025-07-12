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
            WindowUtils.MaximizeWindow(Application.Current.Windows.FirstOrDefault());
#endif
    }

    private void SetButtonChecked(SfButton checkedButton)
    {
        btnShowArticle.IsChecked = checkedButton == btnShowArticle;
        btnShowUser.IsChecked = checkedButton == btnShowUser;
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
            await DisplayAlert("Error", $"No se pudo cargar la secci�n de art�culos: {ex.Message}", "OK");
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
            await DisplayAlert("Error", $"No se pudo cargar la secci�n de usuarios: {ex.Message}", "OK");
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
            await DisplayAlert("Error", $"No se pudo cargar la configuraci�n: {ex.Message}", "OK");
        }
    }

    private async void OnExitButtonClicked(object sender, EventArgs e)
    {
        try
        {
            Application.Current!.MainPage = new NavigationPage(new LoginPage());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo cerrar la sesi�n: {ex.Message}", "OK");
        }
    }
}