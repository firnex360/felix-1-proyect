namespace felix1;

public partial class AdminSectionMainVisual : ContentPage
{
    public AdminSectionMainVisual()
    {
        InitializeComponent();
        RightPanel.Content = new ListArticleVisual();
	}

	private void OnShowArticle(object sender, EventArgs e)
	{
		//RightPanel.Content = new CreateArticleVisual(); // Load the Article view into the placeholder
	}

	private void OnShowUser(object sender, EventArgs e)
	{
        RightPanel.Content = new ListUserVisual(); 
	}

	private void OnShowTable(object sender, EventArgs e)
	{
		//RightPanel.Content = new Table(); // Load the Table view into the placeholder
	}



}