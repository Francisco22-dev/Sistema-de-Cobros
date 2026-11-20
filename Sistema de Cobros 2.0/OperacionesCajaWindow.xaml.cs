using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public partial class OperacionesCajaWindow : Window
    {
        private readonly MovimientoCajaService _movimientoService = new MovimientoCajaService();
        private readonly TasaCambioService _tasaService = new TasaCambioService();
        private readonly DevolucionService _devolucionService = new DevolucionService();

        private Pago? _pagoSeleccionadoParaDevolver;

        public OperacionesCajaWindow()
        {
            InitializeComponent();
        }

        // ========================================================
        // BÚSQUEDA Y SELECCIÓN DE RECIBO EN CONCRETO
        // ========================================================

        private void TxtBuscarRecibo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BuscarReciboParaDevolver();
        }

        private void BtnBuscarRecibo_Click(object sender, RoutedEventArgs e)
        {
            BuscarReciboParaDevolver();
        }

        private void BuscarReciboParaDevolver()
        {
            string busqueda = txtBuscarRecibo.Text.Trim();
            if (string.IsNullOrWhiteSpace(busqueda))
            {
                AlertaWindow.MostrarAdvertencia("Ingrese el número de recibo oficial (Ej: REC-20261114-00004).");
                return;
            }

            try
            {
                var pago = _devolucionService.BuscarPagoParaDevolucion(busqueda);

                if (pago == null)
                {
                    bdrReciboEncontrado.Visibility = Visibility.Collapsed;
                    _pagoSeleccionadoParaDevolver = null;
                    AlertaWindow.MostrarAdvertencia($"No se encontró ningún recibo registrado con el número '{busqueda}'.");
                    return;
                }

                if (pago.Estado == "Devuelto" || pago.Estado == "Anulado")
                {
                    bdrReciboEncontrado.Visibility = Visibility.Collapsed;
                    _pagoSeleccionadoParaDevolver = null;
                    AlertaWindow.MostrarInfo($"El recibo '{pago.NumeroRecibo}' ya se encuentra marcado como {pago.Estado.ToUpper()}.");
                    return;
                }

                // Recibo válido encontrado: Se muestra en la tarjeta
                _pagoSeleccionadoParaDevolver = pago;
                bdrReciboEncontrado.Visibility = Visibility.Visible;
                txtInfoReciboCabecera.Text = $"Recibo: {pago.NumeroRecibo} • Alumno C.I.: {pago.EstudianteCedula}";
                txtInfoReciboCurso.Text = $"Fecha: {pago.FechaPago:dd/MM/yyyy hh:mm tt} • Estado: {pago.Estado}";
                txtInfoReciboMonto.Text = $"Total Cobrado: {pago.Total:N2} {pago.MonedaUI}";

                // Auto-completar campos de monto y moneda
                txtMontoVuelto.Text = pago.Total.ToString("N2");
                cmbMonedaVuelto.SelectedIndex = (pago.MonedaCodigo == "BCV" || pago.MonedaCodigo == "USD" || pago.MonedaCodigo == "$") ? 1 : 0;
                txtMotivoVuelto.Text = $"Devolución total del recibo {pago.NumeroRecibo}";
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al buscar recibo: {ex.Message}");
            }
        }

        // ========================================================
        // PROCESAR REEMBOLSO O VUELTO ORDINARIO
        // ========================================================

        private void BtnProcesarVuelto_Click(object sender, RoutedEventArgs e)
        {
            var formato = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

            if (!decimal.TryParse(txtMontoVuelto.Text.Trim(), NumberStyles.Number, formato, out decimal monto) || monto <= 0)
            {
                AlertaWindow.MostrarAdvertencia("Ingrese un monto válido mayor a cero para la devolución.");
                return;
            }

            string motivo = txtMotivoVuelto.Text.Trim();
            if (string.IsNullOrWhiteSpace(motivo) || motivo.Length < 5)
            {
                AlertaWindow.MostrarAdvertencia("Por favor indique detalladamente el motivo de la salida o devolución (mínimo 5 caracteres).");
                return;
            }

            string metodoSalida = ((ComboBoxItem)cmbMetodoVuelto.SelectedItem).Content.ToString()!;
            string monedaUI = ((ComboBoxItem)cmbMonedaVuelto.SelectedItem).Content.ToString()!;
            string monedaBD = (monedaUI == "$" || monedaUI == "BCV") ? "BCV" : "BS";

            // CASO 1: SE SELECCIONÓ UN RECIBO EN CONCRETO PARA ANULAR
            if (_pagoSeleccionadoParaDevolver != null)
            {
                bool confirmar = AlertaWindow.SolicitarConfirmacion(
                    $"¿Está seguro de anular el recibo '{_pagoSeleccionadoParaDevolver.NumeroRecibo}'?\n\n" +
                    $"• Estado pasará a: DEVUELTO\n" +
                    $"• Monto extraído de caja: {monto:N2} {monedaUI} ({metodoSalida})\n" +
                    $"• La deuda del estudiante volverá a reflejarse en morosidad.",
                    "Confirmar Reembolso");

                if (!confirmar) return;

                try
                {
                    _devolucionService.AnularReciboYReembolsar(_pagoSeleccionadoParaDevolver.NumeroRecibo, motivo, metodoSalida);
                    AlertaWindow.MostrarExito($"El recibo '{_pagoSeleccionadoParaDevolver.NumeroRecibo}' fue anulado (DEVUELTO) y el egreso se descontó de la caja.");
                    this.Close();
                    return;
                }
                catch (Exception ex)
                {
                    AlertaWindow.MostrarError(ex.Message);
                    return;
                }
            }

            // CASO 2: VUELTO ORDINARIO DE CAJA (SIN RECIBO ESPECÍFICO)
            if (!AlertaWindow.SolicitarConfirmacion($"¿Confirmar salida física de {monto:N2} {monedaUI} en {metodoSalida}?", "Salida de Dinero"))
                return;

            try
            {
                _movimientoService.RegistrarEgreso(monto, monedaBD, "Vueltos y Reembolsos", motivo, metodoSalida);
                AlertaWindow.MostrarExito("Salida de dinero registrada y descontada de la caja exitosamente.");
                this.Close();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError(ex.Message);
            }
        }

        // ========================================================
        // CAMBIO DE DIVISAS
        // ========================================================

        private void BtnProcesarCambio_Click(object sender, RoutedEventArgs e)
        {
            var formato = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

            if (!decimal.TryParse(txtMontoEntra.Text.Trim(), NumberStyles.Number, formato, out decimal montoEntra) || montoEntra <= 0 ||
                !decimal.TryParse(txtMontoSale.Text.Trim(), NumberStyles.Number, formato, out decimal montoSale) || montoSale <= 0)
            {
                AlertaWindow.MostrarAdvertencia("Verifique que ambos montos de cambio sean válidos.");
                return;
            }

            string monedaEntraUI = ((ComboBoxItem)cmbMonedaEntra.SelectedItem).Content.ToString()!;
            string monedaSaleUI = ((ComboBoxItem)cmbMonedaSale.SelectedItem).Content.ToString()!;

            if (monedaEntraUI == monedaSaleUI)
            {
                AlertaWindow.MostrarAdvertencia("La moneda que entra no puede ser la misma que sale.");
                return;
            }

            string metodoEntra = ((ComboBoxItem)cmbMetodoEntra.SelectedItem).Content.ToString()!;
            string metodoSale = ((ComboBoxItem)cmbMetodoSale.SelectedItem).Content.ToString()!;

            string monedaEntraBD = monedaEntraUI == "$" ? "BCV" : "BS";
            string monedaSaleBD = monedaSaleUI == "$" ? "BCV" : "BS";

            try
            {
                _movimientoService.RegistrarCambioMoneda(montoEntra, monedaEntraBD, metodoEntra, montoSale, monedaSaleBD, metodoSale, txtMotivoCambio.Text.Trim());
                AlertaWindow.MostrarExito("Operación de cambio de divisas registrada correctamente.");
                this.Close();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError(ex.Message);
            }
        }

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
                decimal tasa = (tasas.ContainsKey("BCV") && tasas["BCV"] > 0) ? tasas["BCV"] : 1m;
                txtMontoSale.Text = (montoEntra * tasa).ToString("N2");
            }
            else if (monEnt == "BS" && monSal == "$")
            {
                decimal tasa = (tasas.ContainsKey("BCV") && tasas["BCV"] > 0) ? tasas["BCV"] : 1m;
                if (tasa > 0) txtMontoSale.Text = (montoEntra / tasa).ToString("N2");
            }
        }
    }
}