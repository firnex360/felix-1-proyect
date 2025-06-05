namespace felix1;

public partial class AdminSectionMainVisual : ContentPage
{
	public AdminSectionMainVisual()
	{
		InitializeComponent();
	}

	private void OnShowArticle(object sender, EventArgs e)
	{
		RightPanel.Content = new Article(); // Load the Article view into the placeholder
	}

	private void OnShowUser(object sender, EventArgs e)
	{
		//RightPanel.Content = new User(); // Load the User view into the placeholder
	}

	private void OnShowTable(object sender, EventArgs e)
	{
		//RightPanel.Content = new Table(); // Load the Table view into the placeholder
	}



}