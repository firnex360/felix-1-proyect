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
        var usersFromDb = AppDbContext.ExecuteSafeAsync(async db =>
            await db.Users
                .Where(u => !u.Deleted)
                .ToListAsync())
            .GetAwaiter().GetResult();

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

    private void OnEditClicked(object sender, EventArgs e)
    {
        // Editar a Larry
        if (sender is ImageButton button && button.BindingContext is User user)
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
        if (sender is ImageButton button && button.BindingContext is User user)
        {
            bool answer = false;
            if (Application.Current?.MainPage != null)
            {
                answer = await Application.Current.MainPage.DisplayAlert(
                    $"¿Estás seguro de que desea eliminar {user.Name}?",
                    "Confirmación",
                    "Sí", "No");
            }

            if (answer)
            {
                await AppDbContext.ExecuteSafeAsync(async db =>
                {
                    var userToDelete = await db.Users.FindAsync(user.Id);

                    if (userToDelete != null)
                    {
                        userToDelete.Deleted = true;
                        await db.SaveChangesAsync();

                        //Ahora sin sniper, supongo que no est� camuflado
                        Dispatcher.Dispatch(LoadUsers);
                    }
                });
            }
        }
    }

    private void OnAvailableCheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.BindingContext is User user)
        {
            AppDbContext.ExecuteSafeAsync(async db =>
            {
                var dbUser = await db.Users.FindAsync(user.Id);
                if (dbUser != null)
                {
                    dbUser.Available = e.Value;
                    await db.SaveChangesAsync();
                }
            }).GetAwaiter().GetResult();
        }
    }

    //Handles the text change event of the search bar to filter users.
    private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            //Reset the DataGrid to show all users
            dataGrid.ItemsSource = Users;
        }
        else
        {
            //Filter the collection
            dataGrid.ItemsSource = Users
                .Where(a => a.Name != null && a.Name.ToLower().Contains(searchText))
                .ToList();
        }
    }
}