using System.Windows;
using System.Windows.Media;

namespace Sistema_de_Cobros_2._0
{
    public partial class AlertaWindow : Window
    {
        public enum Tipo { Informacion, Exito, Advertencia, Error, Confirmacion }
        public bool Resultado { get; private set; } = false;

        public AlertaWindow(string mensaje, string titulo, Tipo tipo)
        {
            InitializeComponent();
            txtMensaje.Text = mensaje;
            txtTitulo.Text = titulo;

            switch (tipo)
            {
                case Tipo.Exito:
                    bdrIcono.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    txtIcono.Text = "✓";
                    btnAceptar.Background = bdrIcono.Background;
                    break;
                case Tipo.Error:
                    bdrIcono.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                    txtIcono.Text = "✕";
                    btnAceptar.Background = bdrIcono.Background;
                    break;
                case Tipo.Advertencia:
                    bdrIcono.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                    txtIcono.Text = "!";
                    btnAceptar.Background = bdrIcono.Background;
                    break;
                case Tipo.Confirmacion:
                    bdrIcono.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                    txtIcono.Text = "?";
                    btnCancelar.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            Resultado = true;
            this.Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            Resultado = false;
            this.Close();
        }

        // --- MÉTODOS ESTÁTICOS PARA USARLA FÁCILMENTE ---
        public static void MostrarExito(string mensaje) => new AlertaWindow(mensaje, "Éxito", Tipo.Exito).ShowDialog();
        public static void MostrarError(string mensaje) => new AlertaWindow(mensaje, "Error", Tipo.Error).ShowDialog();
        public static void MostrarAdvertencia(string mensaje) => new AlertaWindow(mensaje, "Advertencia", Tipo.Advertencia).ShowDialog();
        public static void MostrarInfo(string mensaje) => new AlertaWindow(mensaje, "Información", Tipo.Informacion).ShowDialog();

        public static bool SolicitarConfirmacion(string mensaje, string titulo = "Confirmar")
        {
            var ventana = new AlertaWindow(mensaje, titulo, Tipo.Confirmacion);
            ventana.ShowDialog();
            return ventana.Resultado;
        }
    }
}