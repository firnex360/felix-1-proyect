namespace felix1;

public partial class ListArticleVisual : ContentView
{
    public ListArticleVisual()
    {
        InitializeComponent();

    }
    
    private void OnCreateArticleClicked(object sender, EventArgs e)
    {
        labeltest.Text = "Button clicked";
    }

}