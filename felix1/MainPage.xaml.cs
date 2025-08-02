using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using felix1.Data;
using felix1.Logic;
using felix1.OrderSection;
namespace felix1;

public partial class MainPage : ContentPage
{

    public MainPage()
    {
        InitializeComponent();

        OnGoToLogin(null, null);
    }
    // private void OnCreateUserClicked(object sender, EventArgs e)
    // {
    //     if (Application.Current != null)
    //     {
    //         //is this important? or just for testing
    //         var newUser = new User
    //         {
    //             Id = 1,
    //             Name = "a",
    //             Username = "a",
    //             Password = "a",
    //             Role = "Admin"
    //         };

    //         //newUser = null;

    //         var createUserWindow = new Window(new CreateUserVisual(newUser));
    //         Application.Current.OpenWindow(createUserWindow);
    //     }
    // }


    private async void OnGoToPageBClicked(object sender, EventArgs e)
    {
        using var db = new AppDbContext();
        var table = new Table { LocalNumber = 1, IsTakeOut = true };
        db.Tables.Add(table);
        db.SaveChanges();
        await Navigation.PushAsync(new AdminSectionMainVisual()); //CHECKING - navigate to example page
    }

    private async void OnGoToLogin(object? sender, EventArgs? e)
    {
        await Navigation.PushAsync(new LoginPage()); //CHECKING - navigate to example page
    }

    private async void OnGoToBalanceClicked(object sender, EventArgs e)
    {
        using var db = new AppDbContext();
        var cashRegister = await db.CashRegisters.FirstOrDefaultAsync(cr => cr.IsOpen);
        if (cashRegister == null)   
        {
            await DisplayAlert("Error", "No hay caja abierta.", "OK");
            return;
        }
        await Navigation.PushAsync(new BalanceVisual(cashRegister));
    }

    private async void OnUpdateCcr(object sender, EventArgs e)
    {
        using var db = new AppDbContext();
        var cashRegister = await db.CashRegisters.FirstOrDefaultAsync(cr => cr.Id == 1);
        if (cashRegister == null)
        {
            await DisplayAlert("Error", "bobo.", "OK");
            return;
        }

        // Update the cash register details as needed
        cashRegister.IsOpen = false;
        db.CashRegisters.Update(cashRegister);
        await db.SaveChangesAsync();

        await Navigation.PushAsync(new BalanceVisual(cashRegister));
    }

    private async void OnSaveOrderTest(object sender, EventArgs e)
    {
        using var db = new AppDbContext();
        var table = new Table { LocalNumber = 1, IsTakeOut = true };
        db.Tables.Add(table);
        db.SaveChanges();
        await Navigation.PushAsync(new CreateCashRegisterVisual()); //CHECKING - navigate to order page
    }

    private void OnSaveUserTest(object sender, EventArgs e)
    {
        using var db = new AppDbContext();

        var newUser = new User
        {
            Name = "Admin",
            Username = "a",
            Password = "a",
            Role = "Admin"
        };
        var newUser2 = new User
        {
            Name = "Mesero",
            Username = "m",
            Password = "m",
            Role = "Mesero"
        };
        var newUser3 = new User
        {
            Name = "Cajero",
            Username = "c",
            Password = "c",
            Role = "Cajero"
        };

        db.Users.Add(newUser);
        db.Users.Add(newUser2);
        db.Users.Add(newUser3);
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

