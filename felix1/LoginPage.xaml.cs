using felix1.Data;
using felix1.Logic;
using felix1.OrderSection;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Windows.Input;

namespace felix1;

public partial class LoginPage : ContentPage, INotifyPropertyChanged
{
    private bool _isLoggingIn;
    public bool IsLoggingIn
    {
        get => _isLoggingIn;
        set
        {
            if (_isLoggingIn != value)
            {
                _isLoggingIn = value;
                OnPropertyChanged(nameof(IsLoggingIn));
                (LoginCommand as Command)?.ChangeCanExecute();
            }
        }
    }

    private string _username;
    public string Username
    {
        get => _username;
        set
        {
            if (_username != value)
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }
    }

    private string _password;
    public string Password
    {
        get => _password;
        set
        {
            if (_password != value)
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }
    }

    public ICommand LoginCommand { get; }

    public LoginPage()
    {
        LoginCommand = new Command(async () => await ExecuteLoginAsync(),
                                () => !IsLoggingIn);
        InitializeComponent();
        BindingContext = this;
    }

    private async Task ExecuteLoginAsync()
    {
        if (IsLoggingIn) return;

        IsLoggingIn = true;

        try
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await DisplayAlert("Error", "Por favor introducir username o password", "OK");
                return;
            }

            using var db = new AppDbContext();
            var usuario = await db.Users.FirstOrDefaultAsync(u =>
                u.Username == Username &&
                u.Password == Password &&
                !u.Deleted);

            if (usuario != null)
            {
                AppSession.CurrentUser = usuario;
                await DisplayAlert("Éxito", $"Bienvenido {usuario.Name}!", "OK");

                // Iba a usar un if else, se salvaron
                Page targetPage = usuario.Role switch
                {
                    "Cajero" => new CreateOrderVisual(),
                    "Admin" => new AdminSectionMainVisual(), //cuando tengan la pagina me avisan
                    "Mesero" => new MainPage(), // Cuando tengan las tabletas me avisan
                    _ => new MainPage() // Default case, could be a generic page or error
                };

                await Navigation.PushAsync(targetPage);
            }
            else
            {
                await DisplayAlert("Error", "Contrase�a o username incorrecto", "OK");
            }
        }
        catch (Exception ex)
        {
            //Nuestro salvador.
        }
        IsLoggingIn = false;
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
}