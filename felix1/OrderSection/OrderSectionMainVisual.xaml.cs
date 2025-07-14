using felix1.Data;
using felix1.Logic;
using Microsoft.Maui.ApplicationModel;


namespace felix1.OrderSection;

public partial class OrderSectionMainVisual : ContentPage
{
    private CashRegister _cashRegister;

    public OrderSectionMainVisual(CashRegister cashRegister)
    {
        InitializeComponent();
        _cashRegister = cashRegister;
        DisplayCashRegisterInfo();
        RightPanel.Content = new ListOrderVisual();
        #if WINDOWS
        var window = Application.Current?.Windows.FirstOrDefault();
        if (window != null)
        {
            WindowUtils.MaximizeWindow(window);
        }
        #endif

    }

    private void DisplayCashRegisterInfo()
    {
        lblCashRegisterInfo.Text = $"Caja #{_cashRegister.Number} " /*+
                                 $"Abierta por: {_cashRegister.Cashier?.Name}\n" +
                                 $"Hora de apertura: {_cashRegister.TimeStarted:dd/MM/yyyy HH:mm}\n"*/;
    }

    private async void OnCloseRegister(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmación",
            $"¿Cerrar la caja?",
            "Sí",
            "No");

        if (!confirm)
            return;

        await AppDbContext.ExecuteSafeAsync(async db =>
        {
            var user = await db.Users.FindAsync(AppSession.CurrentUser.Id);
            var register = await db.CashRegisters.FindAsync(_cashRegister.Id);

            if (register != null)
            {
                register.Cashier = user;
                register.IsOpen = false;
                register.TimeFinish = DateTime.Now;
                db.CashRegisters.Update(register);
                await db.SaveChangesAsync();
            }

            Dispatcher.Dispatch( () =>
            {
                /*Navigation.PopAsync();
                var balanceVisual = new BalanceVisual(register);
                Application.Current!.MainPage = new NavigationPage(balanceVisual);*/
                Navigation.PopAsync();
                var balanceVisual = new LoginPage();
                Application.Current!.MainPage = new NavigationPage(balanceVisual);
            });
        });
    }

    private async void OnCloseSesion(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlert(
            "Confirmación",
            "¿Estás seguro que deseas cerrar sesión?",
            "Sí",
            "No");

        if (!confirm)
            return;

        AppSession.CurrentUser = null!;

        var loginPage = new LoginPage();
        Application.Current!.MainPage = new NavigationPage(loginPage);
    }
    private async void OnPaymentButtonClicked(object sender, EventArgs e)
    {
        // Crear un pedido de prueba con artículos reales
        var testOrder = new Order
        {
            Id = 999,
            Date = DateTime.Now,
            Items = new List<OrderItem>
        {
            new OrderItem
            {
                Article = new Article { Name = "Pollo Frito" },
                Quantity = 1,
                UnitPrice = 250.50m
            },
            new OrderItem
            {
                Article = new Article { Name = "Sandía" },
                Quantity = 2,
                UnitPrice = 22.25m
            }
        }
        };

        // Mostrar pantalla de pago
        var paymentPage = new PaymentVisual(testOrder);

        if (Navigation != null)
        {
            await Navigation.PushAsync(paymentPage);
        }
        else
        {
            await Application.Current!.MainPage!.DisplayAlert("Error", "No se puede navegar a la página de pago", "OK");
        }
    }

    private async void OnTestRefundClicked(object sender, EventArgs e)
    {
        // Create fake articles
        var articles = new List<Article>
    {
        new Article { Id = 1, Name = "Burger", PriPrice = 9.99f, Category = ArticleCategory.Principal },
        new Article { Id = 2, Name = "Fries", PriPrice = 3.99f, Category = ArticleCategory.Secundario },
        new Article { Id = 3, Name = "Soda", PriPrice = 1.99f, Category = ArticleCategory.Bebidas },
        new Article { Id = 4, Name = "Ice Cream", PriPrice = 4.50f, Category = ArticleCategory.Postres }
    };

        // Create fake order items
        var items = new List<OrderItem>
    {
        new OrderItem { Article = articles[0], Quantity = 2, UnitPrice = (decimal)articles[0].PriPrice },
        new OrderItem { Article = articles[1], Quantity = 1, UnitPrice = (decimal)articles[1].PriPrice },
        new OrderItem { Article = articles[2], Quantity = 3, UnitPrice = (decimal)articles[2].PriPrice },
        new OrderItem { Article = articles[3], Quantity = 1, UnitPrice = (decimal)articles[3].PriPrice }
    };

        // Create fake order
        var testOrder = new Order
        {
            Id = 999,
            OrderNumber = 12345,
            Date = DateTime.Now.AddHours(-1),
            Items = items,
            Table = new Table { Id = 1, LocalNumber = 5 },
            Waiter = new User { Id = 1, Name = "Test Waiter" },
            CashRegister = _cashRegister
        };

        // Open the refund visual with the test order
        var refundPage = new RefundVisual(testOrder);
        await Navigation.PushAsync(refundPage);
    }

}