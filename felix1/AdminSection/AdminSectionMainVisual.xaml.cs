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
        btnShowTable.IsChecked = checkedButton == btnShowTable;
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

    private void OnShowTable(object sender, EventArgs e)
    {
        SetButtonChecked(btnShowTable);
        //RightPanel.Content = new Table(); // Load the Table view into the placeholder
    }

    private void OnExitButtonClicked(object sender, EventArgs e)
    {
        // Example: Close the current window or navigate to a login/main page
        Application.Current?.MainPage?.DisplayAlert("Salir", "Has hecho clic en Salir.", "OK");
        // You can add your actual exit/logout logic here
    }
}