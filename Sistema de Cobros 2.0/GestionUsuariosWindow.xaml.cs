using System.Windows;
using System.Windows.Controls;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    /// <summary>
    /// Lógica de interacción para GestionUsuariosWindow.xaml
    /// </summary>
    public partial class GestionUsuariosWindow : Window
    {
        private readonly UsuarioService _usuarioService;
        private Usuario? _usuarioSeleccionado;

        public GestionUsuariosWindow()
        {
            InitializeComponent();
            _usuarioService = new UsuarioService();
            CargarUsuarios();
            LimpiarFormulario();
        }

        private void CargarUsuarios()
        {
            dgUsuarios.ItemsSource = _usuarioService.ObtenerUsuarios();
        }

        private void LimpiarFormulario()
        {
            _usuarioSeleccionado = null;
            txtTituloFormulario.Text = "Nuevo Usuario";
            txtUsername.Clear();
            txtUsername.IsEnabled = true;
            txtNombreCompleto.Clear();
            txtPassword.Clear();
            cmbRol.SelectedIndex = 0;
            btnDesactivar.IsEnabled = false;
            btnEliminar.IsEnabled = false;
        }

        private void DgUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUsuarios.SelectedItem is Usuario usuario)
            {
                _usuarioSeleccionado = usuario;
                txtTituloFormulario.Text = "Editar Usuario";
                txtUsername.Text = usuario.Username;
                txtUsername.IsEnabled = false;
                txtNombreCompleto.Text = usuario.NombreCompleto;
                txtPassword.Clear();
                cmbRol.SelectedItem = cmbRol.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == usuario.Rol) ?? cmbRol.Items[0];

                bool esAdmin = usuario.Rol == "Admin";
                btnDesactivar.IsEnabled = !esAdmin;
                btnEliminar.IsEnabled = !esAdmin;
                btnDesactivar.Content = usuario.Activo ? "Desactivar" : "Activar";
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_usuarioSeleccionado == null)
                {
                    // Crear nuevo usuario
                    _usuarioService.CrearUsuario(
                        txtUsername.Text.Trim(),
                        txtPassword.Password,
                        txtNombreCompleto.Text.Trim(),
                        ((ComboBoxItem)cmbRol.SelectedItem!).Content.ToString()!
                    );
                }
                else
                {
                    // Editar usuario existente
                    string? nuevaPassword = string.IsNullOrEmpty(txtPassword.Password) ? null : txtPassword.Password;
                    _usuarioService.ActualizarUsuario(
                        _usuarioSeleccionado.Username,
                        txtNombreCompleto.Text.Trim(),
                        ((ComboBoxItem)cmbRol.SelectedItem!).Content.ToString()!,
                        nuevaPassword
                    );
                }

                CargarUsuarios();
                LimpiarFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnDesactivar_Click(object sender, RoutedEventArgs e)
        {
            if (_usuarioSeleccionado == null) return;

            try
            {
                if (_usuarioSeleccionado.Activo)
                    _usuarioService.DesactivarUsuario(_usuarioSeleccionado.Username);
                else
                    _usuarioService.ActivarUsuario(_usuarioSeleccionado.Username);

                CargarUsuarios();
                LimpiarFormulario();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_usuarioSeleccionado == null) return;

            var resultado = MessageBox.Show(
                $"¿Está seguro de eliminar al usuario '{_usuarioSeleccionado.Username}'?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                    _usuarioService.EliminarUsuario(_usuarioSeleccionado.Username);
                    CargarUsuarios();
                    LimpiarFormulario();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }
    }
}
