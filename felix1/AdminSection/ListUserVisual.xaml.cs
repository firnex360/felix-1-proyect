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
        var usersFromDb = db.Users
            .Where(u => !u.Deleted)
            .ToList();

        Users.Clear();
        foreach (var user in usersFromDb)
            Users.Add(user);
    }


    private void OnCreateUserWindowClicked(object sender, EventArgs e)
    {
        // Get display size
        // var displayInfo = DeviceDisplay.Current.MainDisplayInfo;

        // var window = new Window(new CreateArticleVisual());

        // window.Height = 700;
        // window.Width = 800;

        // // Center the window
        // window.X = (displayInfo.Width / displayInfo.Density - window.Width) / 2;
        // window.Y = (displayInfo.Height / displayInfo.Density - window.Height) / 2;

        // Application.Current?.OpenWindow(window);
        
        labeltest.Text = "hasnt been implemented yet";
    }


    private void OnViewClicked(object sender, EventArgs e)
    {
        // Dummy function for View button
        labeltest.Text = "View clicked";
    }

    private void OnEditClicked(object sender, EventArgs e)
    {
        // Dummy function for Edit button
        labeltest.Text = "Edit clicked";
    }

    private void OnDeleteClicked(object sender, EventArgs e)
    {
        // Dummy function for Delete button
        labeltest.Text = "Delete clicked";
    }

}