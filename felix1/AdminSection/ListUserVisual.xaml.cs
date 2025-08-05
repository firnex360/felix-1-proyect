using System.Collections.ObjectModel;
using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Maui.DataGrid;
using Syncfusion.Maui.Popup;

namespace felix1;

public partial class ListUserVisual : ContentView
{
    public static ListUserVisual? Instance { get; private set; }

    public ObservableCollection<User> Users { get; set; } = new();

    public ListUserVisual()
    {
        InitializeComponent();
        BindingContext = this;
        Instance = this;
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

    public void ReloadUsers() // PUBLIC method to allow external refresh
    {
        LoadUsers();
    }

    private void OnCreateUserWindowClicked(object sender, EventArgs e)
    {
        // Create the popup
        var popup = new SfPopup
        {
            WidthRequest = 800,
            HeightRequest = 700,
            ShowFooter = false,
            ShowCloseButton = true,
            ShowHeader = false,
            StaysOpen = true,
            PopupStyle = new PopupStyle
            {
                MessageBackground = Colors.White,
                HeaderBackground = Colors.Transparent,
                HeaderTextColor = Colors.Black,
                CornerRadius = new CornerRadius(10)
            }
        };

        // Use the converted CreateUserVisual (now a ContentView)
        var createUserView = new CreateUserVisual();
        
        // Try setting content directly without DataTemplate first
        try 
        {
            // Some versions of Syncfusion popup support direct content assignment
            var contentProperty = popup.GetType().GetProperty("Content");
            if (contentProperty != null && contentProperty.CanWrite)
            {
                contentProperty.SetValue(popup, createUserView);
                createUserView.SetPopupReference(popup);
            }
            else
            {
                // Fallback to ContentTemplate
                createUserView.SetPopupReference(popup);
                popup.ContentTemplate = new DataTemplate(() => createUserView);
            }
        }
        catch
        {
            // Fallback to ContentTemplate
            createUserView.SetPopupReference(popup);
            popup.ContentTemplate = new DataTemplate(() => createUserView);
        }

        // Handle when popup is closed to reload users
        popup.Closed += (s, args) =>
        {
            ReloadUsers();
        };

        // Show the popup
        popup.Show();
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

            // Create the popup for editing
            var popup = new SfPopup
            {
                WidthRequest = 800,
                HeightRequest = 700,
                ShowFooter = false,
                ShowHeader = false,
                ShowCloseButton = true,
                StaysOpen = true,
                PopupStyle = new PopupStyle
                {
                    MessageBackground = Colors.White,
                    HeaderBackground = Colors.Transparent,
                    HeaderTextColor = Colors.Black,
                    CornerRadius = new CornerRadius(10)
                }
            };

            // Use the converted CreateUserVisual (now a ContentView) with the user to edit
            var createUserView = new CreateUserVisual(userToEdit);
            createUserView.SetPopupReference(popup);
            
            // Set the ContentView directly as content
            popup.ContentTemplate = new DataTemplate(() => createUserView);

            // Handle when popup is closed to reload users
            popup.Closed += (s, args) =>
            {
                ReloadUsers();
            };

            // Show the popup
            popup.Show();
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