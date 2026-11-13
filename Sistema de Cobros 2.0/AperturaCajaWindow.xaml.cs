using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class AperturaCajaWindow : Window
    {
        private readonly CajaService _cajaService = new CajaService();
        public bool CajaAbiertaExitosamente { get; private set; } = false;

        public AperturaCajaWindow()
        {
            InitializeComponent();
            txtSaldoInicial.Focus();
            txtSaldoInicial.SelectAll();
        }

        private void BtnAbrir_Click(object sender, RoutedEventArgs e)
        {
            var cultura = new CultureInfo("es-VE");
            if (!decimal.TryParse(txtSaldoInicial.Text.Trim(), NumberStyles.Any, cultura, out decimal fondo) || fondo < 0)
            {
                AlertaWindow.MostrarAdvertencia("Por favor ingrese un monto inicial válido (Ej: 20,00 o 0).");
                return;
            }

            string monedaUI = (cmbMonedaInicial.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "$";
            string monedaBD = monedaUI == "$" ? "USD" : "VES";

            try
            {
                _cajaService.AbrirCajaConFondo(fondo, monedaBD);
                CajaAbiertaExitosamente = true;
                this.Close();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError(ex.Message);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}