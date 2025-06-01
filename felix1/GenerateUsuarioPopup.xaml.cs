namespace felix1
{
    public partial class GeneraeUsuarioPopup : ContentPage
    {
        public GeneraeUsuarioPopup()
        {
            InitializeComponent();

            // Asignar el evento Clicked al botón Agregar Usuario
            btnAgregarUsuario.Clicked += OnAgregarUsuarioClicked;


            SetupKeyboardNavigation();
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            Device.BeginInvokeOnMainThread(() =>
            {
                entryNombre.Focus();
            });
        }


        private void OnClosePopupClicked(object sender, EventArgs e)
        {
            Navigation.PopModalAsync();
            //await Navigation.PopModalAsync()
        }

        private void SetupKeyboardNavigation()
        {
            entryNombre.Completed += (s, e) => entryUsername.Focus();
            entryUsername.Completed += (s, e) => entryPassword.Focus();
            entryPassword.Completed += (s, e) => entryCPassword.Focus();
            entryCPassword.Completed += (s, e) => pickerRol.Focus();

            pickerRol.Unfocused += (s, e) => btnAgregarUsuario.Focus();
        }

        private void OnAgregarUsuarioClicked(object sender, EventArgs e)
        {
            // Tienes que completar todos los campos o si no viene Larry
            if (string.IsNullOrWhiteSpace(entryNombre.Text) ||
                string.IsNullOrWhiteSpace(entryUsername.Text) ||
                string.IsNullOrWhiteSpace(entryPassword.Text) ||
                pickerRol.SelectedItem == null)
            {
                DisplayAlert("Error", "Completar todos los campos", "OK");
                return;
            }

            // Tienes que tener la misma contraseña o si no viene Larry
            if (entryPassword.Text != entryCPassword.Text)
            {
                DisplayAlert("Error", "Las contraseñas no coinciden", "OK");
                return;
            }

            // Crear a Larry
            Logic.User nuevoUsuario = new Logic.User
            {
                Name = entryNombre.Text,
                Username = entryUsername.Text,
                Password = entryPassword.Text,
                Role = pickerRol.SelectedItem.ToString(),
                Available = true,
                Deleted = false
            };


            DisplayAlert("Éxito", "Usuario agregado correctamente", "OK");


            // Cerrar el popup
            Navigation.PopModalAsync();
        }
    }
}
