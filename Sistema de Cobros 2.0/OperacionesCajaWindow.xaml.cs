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

        private void RecalcularCambio(object sender, EventArgs e)
        {
            if (txtMontoSale == null || cmbMonedaEntra == null || cmbMonedaSale == null) return;
            if (!decimal.TryParse(txtMontoEntra.Text.Replace(".", ","), out decimal montoEntra)) { txtMontoSale.Text = "0,00"; return; }

            string monEnt = ((ComboBoxItem)cmbMonedaEntra.SelectedItem).Content.ToString()!;
            string monSal = ((ComboBoxItem)cmbMonedaSale.SelectedItem).Content.ToString()!;
            var tasas = _tasaService.ObtenerTasasDelDia();

            // Si son iguales, es 1 a 1
            if (monEnt == monSal) { txtMontoSale.Text = montoEntra.ToString("N2"); return; }

            // Si mete $ y quiere BS (Multiplica)
            if (monEnt == "$" && monSal == "BS") txtMontoSale.Text = (montoEntra * tasas["BCV"]).ToString("N2");
            // Si mete BS y quiere $ (Divide)
            else if (monEnt == "BS" && monSal == "$") txtMontoSale.Text = (montoEntra / tasas["BCV"]).ToString("N2");
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

            try
            {
                _movimientoService.RegistrarEgreso(monto, monedaBD, "Vueltos y Reembolsos", txtMotivoVuelto.Text.Trim());
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

            if (monedaEntraUI == monedaSaleUI)
            {
                AlertaWindow.MostrarAdvertencia("La moneda que entra no puede ser la misma que sale.");
                return;
            }

            string monedaEntraBD = monedaEntraUI == "$" ? "BCV" : "BS";
            string monedaSaleBD = monedaSaleUI == "$" ? "BCV" : "BS";

            try
            {
                _movimientoService.RegistrarCambioMoneda(montoEntra, monedaEntraBD, montoSale, monedaSaleBD, txtMotivoCambio.Text.Trim());
                AlertaWindow.MostrarExito("Operación de cambio de divisas registrada correctamente.");
                this.Close();
            }
            catch (Exception ex) { AlertaWindow.MostrarError(ex.Message); }
        }
    }
}