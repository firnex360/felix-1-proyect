using felix1.Data;
using felix1.Logic;

namespace felix1;

public partial class CreateArticleVisual : ContentPage
{
    private Article? editingArticle = null;

    public CreateArticleVisual(Article? articleToEdit = null)
    {
        InitializeComponent();

        // Populate Picker with enum values
        pckCategory.ItemsSource = Enum.GetValues(typeof(ArticleCategory))
                                         .Cast<ArticleCategory>()
                                         .Select(c => c.ToString())
                                         .ToList();

        // Checking if an article is being edited
        if (articleToEdit != null)
        {
            editingArticle = articleToEdit;

            // Pre-fill the fields
            txtName.Text = editingArticle.Name;
            txtPrice.Text = editingArticle.PriPrice.ToString();
            txtSecondaryPrice.Text = editingArticle.SecPrice.ToString();
            txtSideDish.IsChecked = editingArticle.IsSideDish;

            pckCategory.SelectedItem = editingArticle.Category.ToString();
        }

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

        if (editingArticle == null)
        {

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
        }
        else
        {
            // UPDATE EXISTING
            var article = db.Articles.FirstOrDefault(a => a.Id == editingArticle.Id);
            if (article != null)
            {
                article.Name = txtName.Text;
                article.PriPrice = float.TryParse(txtPrice.Text, out var pri) ? pri : 0f;
                article.SecPrice = float.TryParse(txtSecondaryPrice.Text, out var sec) ? sec : 0f;
                article.Category = parsed ? categoryEnum : ArticleCategory.Other;
                article.IsSideDish = txtSideDish.IsChecked;

                db.Articles.Update(article);
            }
        }
        db.SaveChanges();
    }

}

