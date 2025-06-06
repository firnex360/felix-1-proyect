namespace felix1;

public partial class CreateArticleVisual : ContentView
{
	public CreateArticleVisual()
	{
		InitializeComponent();
	}
	
	private void OnShowA(object sender, EventArgs e)
	{
		RightPanelA.IsVisible = true;
		RightPanelB.IsVisible = false;
	}

	private void OnShowB(object sender, EventArgs e)
	{
		RightPanelA.IsVisible = false;
		RightPanelB.IsVisible = true;
	}
}