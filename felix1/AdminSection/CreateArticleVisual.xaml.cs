using felix1.Data;
using felix1.Logic;

namespace felix1;

public partial class CreateArticleVisual : ContentView
{
	public CreateArticleVisual()
	{
		InitializeComponent();

		
        // Populate Picker with enum values
        pckCategory.ItemsSource = Enum.GetValues(typeof(ArticleCategory))
                                         .Cast<ArticleCategory>()
                                         .Select(c => c.ToString())
                                         .ToList();
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

	private void OnSaveArticle(object sender, EventArgs e)
	{
		using var db = new AppDbContext();

        var selectedCategory = pckCategory.SelectedItem?.ToString();
        var parsed = Enum.TryParse<ArticleCategory>(selectedCategory, out var categoryEnum);

		var newArticle = new Article
		{
			Name = txtName.Text,
			PriPrice = txtPrice.Text != null ? float.Parse(txtPrice.Text) : 0f,
			SecPrice = txtSecondaryPrice.Text != null ? float.Parse(txtSecondaryPrice.Text) : 0f,
			Category = parsed ? categoryEnum : ArticleCategory.Other,
			IsDeleted = false,
			IsSideDish = txtSideDish.IsChecked,
		};

		db.Articles.Add(newArticle);
		db.SaveChanges();
	}
}