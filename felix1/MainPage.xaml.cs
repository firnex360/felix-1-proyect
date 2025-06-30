using System.Data.Common;
using felix1.Data;
using felix1.Logic;
using felix1.OrderSection;
namespace felix1;

public partial class MainPage : ContentPage
{

    int count = 0;


    public MainPage()
    {
        InitializeComponent();
    }
    private void OnCreateUserClicked(object sender, EventArgs e)
    {
        if (Application.Current != null)
        {

            var newUser = new User
            {
                Id = 1,
                Name = "a",
                Username = "a",
                Password = "a",
                Role = "Admin"
            };

            //newUser = null;

            var createUserWindow = new Window(new CreateUserVisual(newUser));
            Application.Current.OpenWindow(createUserWindow);
        }
    }


    private async void OnGoToPageBClicked(object sender, EventArgs e)
    {
        using var db = new AppDbContext();
        var table = new Table { LocalNumber = 1, IsTakeOut = true };
        db.Tables.Add(table);
        db.SaveChanges();
        await Navigation.PushAsync(new AdminSectionMainVisual()); //CHECKING - navigate to example page
    }

    private async void OnGoToLogin(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage()); //CHECKING - navigate to example page
    }

    private async void OnSaveOrderTest(object sender, EventArgs e)
    {
        using var db = new AppDbContext();
        var table = new Table { LocalNumber = 1, IsTakeOut = true };
        db.Tables.Add(table);
        db.SaveChanges();
        await Navigation.PushAsync(new CreateOrderVisual()); //CHECKING - navigate to order page
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
            // Name = "Sample Article",
            // PriPrice = 10.99f,
            // SecPrice = 8.99f,
            // Category = ArticleCategory.MainDish,
            // IsDeleted = false,
            // IsSideDish = false
        };

        db.Articles.Add(newArticle);
        db.SaveChanges();
    }

    private void ClickToShowPopup_Clicked(object sender, EventArgs e)
    {
        popup.Show();
    }

}

