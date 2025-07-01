using felix1.Data;
using felix1.Logic;
using felix1.OrderSection;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Windows.Input;
using Syncfusion.Maui.Buttons;

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

    private double _progressValue;
    public double ProgressValue
    {
        get => _progressValue;
        set
        {
            if (_progressValue != value)
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public string ProgressText => $"{(int)(ProgressValue * 100)}%";

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
        ProgressValue = 0;

        try
        {
            await Task.Delay(100);
            ProgressValue = 0.3;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await DisplayAlert("Error", "Por favor introducir username o password", "OK");
                return;
            }

            await Task.Delay(100);
            ProgressValue = 0.6;

            using var db = new AppDbContext();
            var usuario = await db.Users.FirstOrDefaultAsync(u =>
                u.Username == Username &&
                u.Password == Password &&
                !u.Deleted);

            await Task.Delay(100);
            ProgressValue = 0.9;

            if (usuario != null)
            {
                AppSession.CurrentUser = usuario;

                //Yo quería poner un if else, pero bueno...
                Page targetPage = usuario.Role switch
                {
                    "Cajero" => new CreateOrderVisual(),
                    "Admin" => new AdminSectionMainVisual(),
                    "Mesero" => new MainPage(), //Cuando tengamos algo se remplaza este MainPage
                    _ => new MainPage()
                };

                Username = string.Empty;
                Password = string.Empty;

                ProgressValue = 1.0;
                await Task.Delay(100);

                //await Navigation.PushAsync(targetPage);
                Application.Current.MainPage = new NavigationPage(targetPage);
            }
            else
            {
                await DisplayAlert("Error", "Contraseña o username incorrecto", "OK");
            }
        }
        catch (Exception ex)
        {
            //Nuestro salvador.
        }
        finally
        {
            IsLoggingIn = false;
            ProgressValue = 0;
        }
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
}