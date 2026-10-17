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
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var formatoEstricto = new System.Globalization.NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = "."
            };

            if (decimal.TryParse(txtBCV.Text.Trim(), System.Globalization.NumberStyles.Number, formatoEstricto, out decimal bcv) &&
                decimal.TryParse(txtEUR.Text.Trim(), System.Globalization.NumberStyles.Number, formatoEstricto, out decimal eur) &&
                decimal.TryParse(txtUSDT.Text.Trim(), System.Globalization.NumberStyles.Number, formatoEstricto, out decimal usdt))
            {
                _tasaService.FijarTasas(bcv, eur, usdt);
                AlertaWindow.MostrarExito("Tasas guardadas exitosamente.");
                this.Close();
            }
            else
            {
                AlertaWindow.MostrarAdvertencia("Por favor verifique que todos los campos tengan números válidos (Ej. 1.250,50).");
            }
        }
    }
}