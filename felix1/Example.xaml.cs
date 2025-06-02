namespace felix1;

public partial class Example : ContentPage
{
	public Example()
	{
		InitializeComponent();
	}

private void OnShowArticle(object sender, EventArgs e)
{
    RightPanel.Content = new Article(); // Load the Article view into the placeholder
}


}