using felix1.Data;
using felix1.Logic;
namespace felix1;

public partial class MainPage : ContentPage
{

    int count = 0;

    public MainPage()
    {
        InitializeComponent();
    }

    private void OnCounterClicked(object sender, EventArgs e)
    {
        count++;

        if (count == 1)
            CounterBtn.Text = $"Clicked {count} time";
        else
            CounterBtn.Text = $"Clicked {count} times";

        SemanticScreenReader.Announce(CounterBtn.Text);
    }


    private async void OnGoToPageBClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new Example()); //CHECKING - navigate to example page
    }

    private void OnSaveUserTest(object sender, EventArgs e)
    {
        using var db = new AppDbContext();

        var newUser = new User
        {
            Name = "John Doe",
            Username = "johnd",
            Password = "secret123",
            Role = "Admin"
        };

        db.Users.Add(newUser);
        db.SaveChanges();
    }

    private void OnSaveArticleTest(object sender, EventArgs e)
    {
        using var db = new AppDbContext();

        var newArticle = new Article
        {
            Name = "Sample Article",
            PriPrice = 10.99f,
            SecPrice = 8.99f,
            Category = ArticleCategory.MainDish,
            IsDeleted = false,
            IsSideDish = false
        };

        db.Articles.Add(newArticle);
        db.SaveChanges();
    }

}

