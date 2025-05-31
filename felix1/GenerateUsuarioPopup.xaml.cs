namespace felix1;

public partial class GeneraeUsuarioPopup : ContentPage
{
    public GeneraeUsuarioPopup()
    {
        InitializeComponent();
    }

    private async void OnClosePopupClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }


}
