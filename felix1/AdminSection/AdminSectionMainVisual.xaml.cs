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

    private void OnShowArticle(object sender, EventArgs e)
    {
        SetButtonChecked(btnShowArticle);
        RightPanel.Content = new ListArticleVisual();
    }

    private void OnShowUser(object sender, EventArgs e)
    {
        SetButtonChecked(btnShowUser);
        RightPanel.Content = new ListUserVisual();
    }

    private void OnShowConfiguration(object sender, EventArgs e)
    {
        SetButtonChecked(btnShowConfiguration);
        RightPanel.Content = new Configuration();
    }

    private async void OnExitButtonClicked(object sender, EventArgs e)
    {

        //Application.Current?.MainPage?.DisplayAlert("Salir", "Has hecho clic en Salir.", "OK");
        await Navigation.PushAsync(new LoginPage()); //CHECKING - navigate to example page
    }
}