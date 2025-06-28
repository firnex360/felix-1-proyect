using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;

namespace felix1;

public partial class CreateArticleVisual : ContentPage
{
    private Article? editingArticle = null;
    public ObservableCollection<SideDishSelectable> SideDishArticles { get; set; } = new();
    //public ObservableCollection<Article> SideDishes { get; set; } = new();

    public class SideDishSelectable : INotifyPropertyChanged
    {
        public Article? Article { get; set; }

        public int Id => Article?.Id ?? 0;
        public string? Name => Article?.Name;

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    //Code reference from: https://learn.microsoft.com/en-us/answers/questions/1479605/how-to-set-an-entry-in-net-maui-to-only-except-num
    private void OnNumericEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        string regex = e.NewTextValue;
        if (String.IsNullOrEmpty(regex))
            return;

        if (!Regex.Match(regex, "^[0-9]+$").Success)
        {
            var entry = sender as Entry;
            if (entry != null)
            {
                entry.Text = string.IsNullOrEmpty(e.OldTextValue) ?
                        string.Empty : e.OldTextValue;
            }
        }
    }

    public CreateArticleVisual(Article? articleToEdit = null)
    {
        InitializeComponent();
        BindingContext = this;

        // Initialize the ObservableCollection for side dishes
        using var db = new AppDbContext();
        var sideDishes = db.Articles
                        .Where(a => a.IsSideDish && !a.IsDeleted)
                        .ToList();

        var selectedIds = new HashSet<int>();

        // Populate Picker with enum values
        pckCategory.ItemsSource = Enum.GetValues(typeof(ArticleCategory))
                                         .Cast<ArticleCategory>()
                                         .Select(c => c.ToString())
                                         .ToList();

        // Checking if an article is being edited
        if (articleToEdit != null)
        {
            editingArticle = db.Articles
                .Where(a => a.Id == articleToEdit.Id)
                .Include(a => a.SideDishes)
                .FirstOrDefault();

            // Pre-fill the fields
            txtCode.Text = editingArticle?.Id.ToString() ?? string.Empty;
            txtName.Text = editingArticle?.Name ?? string.Empty;
            txtPrice.Text = editingArticle?.PriPrice.ToString() ?? string.Empty;
            txtSecondaryPrice.Text = editingArticle?.SecPrice.ToString() ?? string.Empty;
            txtSideDish.IsChecked = editingArticle?.IsSideDish ?? false;

            pckCategory.SelectedItem = editingArticle?.Category.ToString() ?? string.Empty;

            selectedIds = editingArticle?.SideDishes?.Select(sd => sd.Id).ToHashSet() ?? new HashSet<int>();

        }

        // Build the observable collection for the grid
        SideDishArticles.Clear();
        foreach (var dish in sideDishes)
        {
            var selectable = new SideDishSelectable
            {
                Article = dish
            };

            selectable.IsSelected = selectedIds.Contains(dish.Id); // triggers PropertyChanged

            SideDishArticles.Add(selectable);
        }


    }



    private async void OnSaveArticle(object sender, EventArgs e)
    {
        // VALIDATION
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            await DisplayAlert("Error", "El campo 'Nombre' es obligatorio.", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtPrice.Text))
        {
            await DisplayAlert("Error", "El campo 'Precio' es obligatorio.", "OK");
            return;
        }

        if (pckCategory.SelectedItem == null)
        {
            await DisplayAlert("Error", "Debe seleccionar una categoría.", "OK");
            return;
        }




        //POPUP CONFIRMATION
        bool confirm = await DisplayAlert(
            "Confirmación",
            editingArticle == null ? "¿Crear este artículo?" : "¿Actualizar este artículo?",
            "Sí",
            "No");

        if (!confirm)
            return;

        using var db = new AppDbContext();

        var selectedCategory = pckCategory.SelectedItem?.ToString();
        var parsed = Enum.TryParse<ArticleCategory>(selectedCategory, out var categoryEnum);

        // Collect selected side dishes
        var selectedSideDishes = SideDishArticles
            .Where(a => a.IsSelected)
            .Select(a => db.Articles.Find(a.Id)) // fetch tracked instances
            .Where(a => a != null)
            .ToList();

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
                SideDishes = selectedSideDishes
                    .Where(a => a != null)
                    .Cast<Article>()
                    .ToList()
            };

            db.Articles.Add(newArticle);

        }
        else
        {
            // UPDATE EXISTING
            var article = db.Articles
                .Include(a => a.SideDishes) // Include side dishes for update
                .FirstOrDefault(a => a.Id == editingArticle.Id);

            if (article != null)
            {
                article.Name = txtName.Text;
                article.PriPrice = float.TryParse(txtPrice.Text, out var pri) ? pri : 0f;
                article.SecPrice = float.TryParse(txtSecondaryPrice.Text, out var sec) ? sec : 0f;
                article.Category = parsed ? categoryEnum : ArticleCategory.Other;
                article.IsSideDish = txtSideDish.IsChecked;

                // Clear and update side dishes
                if (article.SideDishes != null)
                {
                    article.SideDishes.Clear();

                    foreach (var sd in selectedSideDishes)
                        if (sd != null)
                            article.SideDishes.Add(sd);
                }

                db.Articles.Update(article);
            }
        }

        db.SaveChanges();
        ListArticleVisual.Instance?.ReloadArticles(); // REFRESH THE LIST
        await DisplayAlert("Éxito", "Artículo guardado correctamente.", "OK");

        CloseThisWindow();

    }

    private void CloseThisWindow()
    {
        if (Application.Current != null)
        {
            foreach (var window in Application.Current.Windows)
            {
                if (window.Page == this)
                {
                    Application.Current.CloseWindow(window);
                    break;
                }
            }
        }
        else
        {
            
        }
    }

    //for the search bar logic
    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            // Reset the DataGrid to show all side dish articles
            sideDishDataGrid.ItemsSource = SideDishArticles;
        }
        else
        {
            // Filter the SideDishArticles collection by Name
            sideDishDataGrid.ItemsSource = SideDishArticles
                .Where(s => s.Name != null && s.Name.ToLower().Contains(searchText))
                .ToList();
        }
    }
}

