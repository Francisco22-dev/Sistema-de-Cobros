using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class OperacionesCajaWindow : Window
    {
        private readonly MovimientoCajaService _movimientoService = new MovimientoCajaService();
        private readonly TasaCambioService _tasaService = new TasaCambioService();

        public OperacionesCajaWindow()
        {
            InitializeComponent();
        }

        private void BtnProcesarVuelto_Click(object sender, RoutedEventArgs e)
        {
            var formato = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

            if (!decimal.TryParse(txtMontoVuelto.Text.Trim(), System.Globalization.NumberStyles.Number, formato, out decimal monto) || monto <= 0)
            {
                AlertaWindow.MostrarAdvertencia("Ingrese un monto válido para el vuelto.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtMotivoVuelto.Text))
            {
                AlertaWindow.MostrarAdvertencia("Por favor indique un motivo (A quién le dio el vuelto).");
                return;
            }

            string monedaUI = ((ComboBoxItem)cmbMonedaVuelto.SelectedItem).Content.ToString()!;
            string monedaBD = monedaUI == "$" ? "BCV" : "BS";
            string metodo = ((ComboBoxItem)cmbMetodoVuelto.SelectedItem).Content.ToString()!; // SE CAPTURA EL METODO

            try
            {
                // SE ENVÍA EL MÉTODO AL REGISTRO
                _movimientoService.RegistrarEgreso(monto, monedaBD, "Vueltos y Reembolsos", txtMotivoVuelto.Text.Trim(), metodo);
                AlertaWindow.MostrarExito("Vuelto registrado y descontado del saldo global.");
                this.Close();
            }
            catch (Exception ex) { AlertaWindow.MostrarError(ex.Message); }
        }

        private void BtnProcesarCambio_Click(object sender, RoutedEventArgs e)
        {
            var formato = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

            if (!decimal.TryParse(txtMontoEntra.Text.Trim(), System.Globalization.NumberStyles.Number, formato, out decimal montoEntra) || montoEntra <= 0 ||
                !decimal.TryParse(txtMontoSale.Text.Trim(), System.Globalization.NumberStyles.Number, formato, out decimal montoSale) || montoSale <= 0)
            {
                AlertaWindow.MostrarAdvertencia("Verifique que ambos montos sean válidos.");
                return;
            }

            string monedaEntraUI = ((ComboBoxItem)cmbMonedaEntra.SelectedItem).Content.ToString()!;
            string monedaSaleUI = ((ComboBoxItem)cmbMonedaSale.SelectedItem).Content.ToString()!;

            string metodoEntra = ((ComboBoxItem)cmbMetodoEntra.SelectedItem).Content.ToString()!;
            string metodoSale = ((ComboBoxItem)cmbMetodoSale.SelectedItem).Content.ToString()!;

            if (monedaEntraUI == monedaSaleUI)
            {
                AlertaWindow.MostrarAdvertencia("La moneda que entra no puede ser la misma que sale.");
                return;
            }

            string monedaEntraBD = monedaEntraUI == "$" ? "BCV" : "BS";
            string monedaSaleBD = monedaSaleUI == "$" ? "BCV" : "BS";

            try
            {
                // SE ENVÍAN LOS MÉTODOS AL REGISTRO DEL CAMBIO
                _movimientoService.RegistrarCambioMoneda(montoEntra, monedaEntraBD, metodoEntra, montoSale, monedaSaleBD, metodoSale, txtMotivoCambio.Text.Trim());
                AlertaWindow.MostrarExito("Operación de cambio de divisas registrada correctamente.");
                this.Close();
            }
            catch (Exception ex) { AlertaWindow.MostrarError(ex.Message); }
        }
        // Estos eventos atrapan cuando escribes o cambias un ComboBox y llaman al cálculo seguro
        private void Input_Changed(object sender, RoutedEventArgs e) => EfectuarCalculoSeguro();

        private void EfectuarCalculoSeguro()
        {
            if (txtMontoSale == null || cmbMonedaEntra == null || cmbMonedaSale == null || !this.IsLoaded) return;

            if (!decimal.TryParse(txtMontoEntra.Text.Replace(".", ","), out decimal montoEntra))
            {
                txtMontoSale.Text = "0,00";
                return;
            }

            string monEnt = ((ComboBoxItem)cmbMonedaEntra.SelectedItem).Content.ToString()!;
            string monSal = ((ComboBoxItem)cmbMonedaSale.SelectedItem).Content.ToString()!;
            var tasas = _tasaService.ObtenerTasasDelDia();

            if (monEnt == monSal)
            {
                txtMontoSale.Text = montoEntra.ToString("N2");
            }
            else if (monEnt == "$" && monSal == "BS")
            {
                txtMontoSale.Text = (montoEntra * tasas["BCV"]).ToString("N2");
            }
            else if (monEnt == "BS" && monSal == "$")
            {
                if (tasas["BCV"] > 0)
                    txtMontoSale.Text = (montoEntra / tasas["BCV"]).ToString("N2");
            }
        }
    }
}