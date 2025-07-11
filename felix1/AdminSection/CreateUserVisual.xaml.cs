﻿using felix1.Data;
using felix1.Logic;

namespace felix1
{
    public partial class CreateUserVisual : ContentPage
    {
        private readonly Logic.User? usuario;

        public CreateUserVisual(Logic.User? usuario = null)
        {
            InitializeComponent();

            this.usuario = usuario;

            btnGuardarUsuario.Clicked += OnGuardarUsuarioClicked;

            SetupKeyboardNavigation();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

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

            Device.BeginInvokeOnMainThread(() => entryNombre.Focus());
        }

        private void SetupKeyboardNavigation()
        {
            entryNombre.Completed += (s, e) => entryUsername.Focus();
            entryUsername.Completed += (s, e) => entryPassword.Focus();
            entryPassword.Completed += (s, e) => entryCPassword.Focus();
            entryCPassword.Completed += (s, e) => btnGuardarUsuario.Focus();
        }

        private async void OnCancelButtonClicked(object sender, EventArgs e)
        {
            Application.Current.CloseWindow(Window);
        }

        private async void OnGuardarUsuarioClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(entryNombre.Text) ||
                string.IsNullOrWhiteSpace(entryUsername.Text) ||
                string.IsNullOrWhiteSpace(entryPassword.Text) ||
                pickerRol.SelectedItem == null)
            {
                await DisplayAlert("Error", "Completar todos los campos", "OK");
                return;
            }

            if (entryPassword.Text != entryCPassword.Text)
            {
                await DisplayAlert("Error", "Las contraseñas no coinciden", "OK");
                return;
            }

            using var db = new AppDbContext();

            var usernameExists = db.Users.Any(u =>
                u.Username.ToLower() == entryUsername.Text.Trim().ToLower() &&
                (usuario == null || u.Id != usuario.Id));

            if (usernameExists)
            {
                await DisplayAlert("Error", "El nombre de usuario ya está en uso. Por favor elija otro.", "OK");
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

                db.Users.Add(newUsuario);
                db.SaveChanges();
                await DisplayAlert("Éxito", "Usuario agregado correctamente", "OK");
            }
            else
            {
                var UsuarioToUpdate = db.Users.FirstOrDefault(u => u.Id == usuario.Id);

                if (UsuarioToUpdate != null)
                {
                    UsuarioToUpdate.Name = entryNombre.Text;
                    UsuarioToUpdate.Username = entryUsername.Text;
                    UsuarioToUpdate.Password = entryPassword.Text;
                    UsuarioToUpdate.Role = pickerRol.SelectedItem.ToString();

                    db.SaveChanges();
                    await DisplayAlert("Éxito", "Usuario modificado correctamente", "OK");
                }
                else
                {
                    await DisplayAlert("Error", "No se encontró el usuario", "OK");
                    return;
                }
            }

            if (Window != null)
            {
                Application.Current.CloseWindow(Window);
            }
            else
            {
                await Navigation.PopModalAsync();
            }
        }
    }
}
