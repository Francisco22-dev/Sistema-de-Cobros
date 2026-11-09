using System.Globalization;
using System.Windows;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class ConfigurarTasaWindow : Window
    {
        private readonly TasaCambioService _tasaService = new TasaCambioService();

        public ConfigurarTasaWindow()
        {
            InitializeComponent();
            txtBCV.Focus();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var formatoEstricto = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

            // Validamos solo el dólar ($) y le enviamos 1 de comodín a los demás para no romper la BD
            if (decimal.TryParse(txtBCV.Text.Trim(), NumberStyles.Number, formatoEstricto, out decimal bcv) && bcv > 0)
            {
                _tasaService.FijarTasas(bcv, 1m, 1m);
                AlertaWindow.MostrarExito("Tasa guardada exitosamente.");
                this.Close();
            }
            else
            {
                AlertaWindow.MostrarAdvertencia("Por favor verifique que la tasa ingresada sea válida (Ej. 45,50).");
            }
        }
    }
}