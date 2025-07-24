using felix1.Data;
using felix1.Logic;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Maui.Popup;

namespace felix1
{
    public partial class CreateUserVisual : ContentView
    {
        private readonly Logic.User? usuario;
        
        // Property to hold popup reference when used in popup mode
        private SfPopup? _parentPopup = null;

        public void SetPopupReference(SfPopup popup)
        {
            _parentPopup = popup;
        }

        public CreateUserVisual(Logic.User? usuario = null)
        {
            InitializeComponent();

            this.usuario = usuario;

            btnGuardarUsuario.Clicked += OnGuardarUsuarioClicked!;

            SetupKeyboardNavigation();
            
            // Initialize the UI based on whether we're editing or creating
            InitializeUI();
        }

        private void InitializeUI()
        {
            if (usuario != null)
            {
                entryNombre.Text = usuario.Name;
                entryUsername.Text = usuario.Username;
                entryPassword.Text = usuario.Password;
                entryCPassword.Text = usuario.Password;
                pickerRol.SelectedItem = usuario.Role;

                btnGuardarUsuario.Text = "Guardar Cambios";
            }
            else
            {
                btnGuardarUsuario.Text = "Agregar Usuario";
            }

            // Focus on the first field
            Dispatcher.Dispatch(() => entryNombre.Focus());
        }

        private void SetupKeyboardNavigation()
        {
            entryNombre.Completed += (s, e) => entryUsername.Focus();
            entryUsername.Completed += (s, e) => entryPassword.Focus();
            entryPassword.Completed += (s, e) => entryCPassword.Focus();
            entryCPassword.Completed += (s, e) => btnGuardarUsuario.Focus();
        }

        private void OnCancelButtonClicked(object sender, EventArgs e)
        {
            CloseThisWindow();
        }

        // Helper methods for displaying alerts since ContentView doesn't have DisplayAlert
        private async Task ShowAlert(string title, string message, string cancel)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(title, message, cancel);
            }
        }

        private async Task<bool> ShowConfirmation(string title, string message, string accept, string cancel)
        {
            if (Application.Current?.MainPage != null)
            {
                return await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
            }
            return false;
        }

        private void CloseThisWindow()
        {
            // If we're in popup mode, close the popup
            if (_parentPopup != null)
            {
                try
                {
                    _parentPopup.IsOpen = false;
                    _parentPopup.Dismiss();
                    Console.WriteLine("Popup dismissed successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error dismissing popup: {ex.Message}");
                }
                return;
            }
            
            Console.WriteLine("No popup reference found - popup was not closed");
        }

        private async void OnGuardarUsuarioClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(entryNombre.Text) ||
                string.IsNullOrWhiteSpace(entryUsername.Text) ||
                string.IsNullOrWhiteSpace(entryPassword.Text) ||
                pickerRol.SelectedItem == null)
            {
                await ShowAlert("Error", "Completar todos los campos", "OK");
                return;
            }

            if (entryPassword.Text != entryCPassword.Text)
            {
                await ShowAlert("Error", "Las contraseñas no coinciden", "OK");
                return;
            }

            try
            {
                var usernameExists = await AppDbContext.ExecuteSafeAsync<bool>(async db =>
                {
                    return await db.Users.AnyAsync(u =>
                        u.Username!.ToLower() == entryUsername.Text.Trim().ToLower() &&
                        (usuario == null || u.Id != usuario.Id));
                });

                if (usernameExists)
                {
                    await ShowAlert("Error", "El nombre de usuario ya está en uso. Por favor elija otro.", "OK");
                    return;
                }

                if (usuario == null)
                {
                    var newUsuario = new Logic.User
                    {
                        Name = entryNombre.Text,
                        Username = entryUsername.Text,
                        Password = entryPassword.Text,
                        Role = pickerRol.SelectedItem.ToString()
                    };

                    await AppDbContext.ExecuteSafeAsync(async db =>
                    {
                        await db.Users.AddAsync(newUsuario);
                        await db.SaveChangesAsync();
                    });

                    await ShowAlert("Éxito", "Usuario agregado correctamente", "OK");
                }
                else
                {
                    await AppDbContext.ExecuteSafeAsync(async db =>
                    {
                        var UsuarioToUpdate = await db.Users.FirstOrDefaultAsync(u => u.Id == usuario.Id);

                        if (UsuarioToUpdate != null)
                        {
                            UsuarioToUpdate.Name = entryNombre.Text;
                            UsuarioToUpdate.Username = entryUsername.Text;
                            UsuarioToUpdate.Password = entryPassword.Text;
                            UsuarioToUpdate.Role = pickerRol.SelectedItem.ToString();

                            await db.SaveChangesAsync();
                            await ShowAlert("Éxito", "Usuario modificado correctamente", "OK");
                        }
                        else
                        {
                            await ShowAlert("Error", "No se encontró el usuario", "OK");
                        }
                    });
                }

                // Refresh the user list if possible
                ListUserVisual.Instance?.ReloadUsers();
                
                CloseThisWindow();
            }
            catch (Exception ex)
            {
                await ShowAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
            }
        }
    }
}