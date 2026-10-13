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
            if (decimal.TryParse(txtUSD.Text.Replace(".", ","), out decimal usd) &&
                decimal.TryParse(txtEUR.Text.Replace(".", ","), out decimal eur) &&
                decimal.TryParse(txtUSDT.Text.Replace(".", ","), out decimal usdt))
            {
                _tasaService.FijarTasas(usd, eur, usdt);
                AlertaWindow.MostrarExito("Tasas guardadas exitosamente.");
                this.Close();
            }
            else
            {
                AlertaWindow.MostrarAdvertencia("Por favor verifique que todos los campos tengan números válidos.");
            }
        }
    }
}