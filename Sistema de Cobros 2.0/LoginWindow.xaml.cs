using System;
using System.Windows;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    /// <summary>
    /// Lógica de interacción para LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly UsuarioService _usuarioService;

        public LoginWindow()
        {
            InitializeComponent();
            _usuarioService = new UsuarioService();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                txtError.Text = "Ingrese usuario y contraseña.";
                return;
            }

            btnLogin.IsEnabled = false;
            btnLogin.Content = "Conectando...";
            txtError.Text = "";

            try
            {
                if (_usuarioService.ValidarCredenciales(username, password, out var usuario))
                {
                    SesionService.Instancia.IniciarSesion(usuario!);
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    txtError.Text = "Usuario o contraseña incorrectos.";
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (InvalidOperationException ex)
            {
                txtError.Text = ex.Message;
            }
            catch (Exception ex)
            {
                // Ahora mostrará la causa REAL del error en la pantalla si algo falla
                txtError.Text = $"Error del sistema: {ex.Message}";
            }
            finally
            {
                btnLogin.IsEnabled = true;
                btnLogin.Content = "Iniciar Sesión";
            }
           
        }
    }
}
