using System.Collections.ObjectModel;
using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;

namespace felix1;

public partial class ListUserVisual : ContentView
{

    public ObservableCollection<User> Users { get; set; } = new();

    public ListUserVisual()
    {
        InitializeComponent();
        BindingContext = this;
        LoadUsers();
    }

    private void LoadUsers()
    {
        using var db = new AppDbContext();
        var usersFromDb = db.Users != null
            ? db.Users.Where(u => !u.Deleted)
            .ToList()
            : new List<User>();

        Users.Clear();
        foreach (var user in usersFromDb)
            Users.Add(user);
    }


    private void OnCreateUserWindowClicked(object sender, EventArgs e)
    {
        // Get display size
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;

        var window = new Window(new CreateUserVisual());

        window.Height = 700;
        window.Width = 800;

        // Center the window
        window.X = (displayInfo.Width / displayInfo.Density - window.Width) / 2;
        window.Y = (displayInfo.Height / displayInfo.Density - window.Height) / 2;

        // Revisa cuando se cierra la ventana, es como un Sniper Monkey
        window.Destroying += (s, args) =>
        {
            LoadUsers();
            // POP!
        };

        Application.Current?.OpenWindow(window);
    }


    private void OnViewClicked(object sender, EventArgs e)
    {
        // Dummy function for View button
        labeltest.Text = "View clicked";
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        // Editar a Larry
        if (sender is Button button && button.BindingContext is User user)
        {
            // Create a deep copy of the user to edit
            var userToEdit = new User
            {
                Id = user.Id,
                Name = user.Name,
                Username = user.Username,
                Password = user.Password, // Note: Be careful with password handling
                Role = user.Role,
                Available = user.Available,
                Deleted = user.Deleted
            };

            // Open edit window with the user object
            var editWindow = new Window(new CreateUserVisual(userToEdit))
            {
                Height = 700,
                Width = 800
            };

            // Center the window
            var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
            editWindow.X = (displayInfo.Width / displayInfo.Density - editWindow.Width) / 2;
            editWindow.Y = (displayInfo.Height / displayInfo.Density - editWindow.Height) / 2;

            // Revisa cuando se cierra la ventana, es como un Sniper Monkey (Creo que ya dije eso)
            editWindow.Destroying += (s, args) =>
            {
                LoadUsers();
                // POP!
            };

            Application.Current?.OpenWindow(editWindow);
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is User user)
        {
            bool answer = await Application.Current.MainPage.DisplayAlert(
                $"�Estas seguro de que desea eliminar {user.Name}?",
                "Confirmaci�n",
                "S�", "No");

            if (answer)
            {
                using var db = new AppDbContext();
                var userToDelete = await db.Users.FindAsync(user.Id);

                if (userToDelete != null)
                {
                    userToDelete.Deleted = true;
                    await db.SaveChangesAsync();

                   //Ahora sin sniper, supongo que no est� camuflado
                    LoadUsers();
                }
            }
        }
    }

}