using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class FlujoCajaWindow : Window
    {
        private readonly MovimientoCajaService _movimientoService = new MovimientoCajaService();
        private readonly TasaCambioService _tasaService = new TasaCambioService();

        // Guardamos los datos puros en memoria para no recalcular la BD a cada rato
        private ResumenFlujo _resumenActual = new ResumenFlujo();
        private Dictionary<string, decimal> _tasasActuales = new Dictionary<string, decimal>();

        public FlujoCajaWindow()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                var categorias = _movimientoService.ObtenerCategorias();
                cmbCategoria.ItemsSource = categorias;
                if (categorias.Count > 0) cmbCategoria.SelectedIndex = 0;

                _tasasActuales = _tasaService.ObtenerTasasDelDia();
                _resumenActual = _movimientoService.CalcularResumenGlobal();

                ActualizarTarjetasVisuales();
                dgEgresos.ItemsSource = _movimientoService.ObtenerTodosLosEgresos();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError(ex.Message);
            }
        }

        private void CmbMonedaDashboard_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtIngresos != null) ActualizarTarjetasVisuales();
        }

        private void ActualizarTarjetasVisuales()
        {
            string monedaSeleccionada = ((ComboBoxItem)cmbMonedaDashboard.SelectedItem).Content.ToString()!;
            decimal divisor = 1m;

            // Extraemos el divisor en base a las tasas de hoy para calcular todo al vuelo
            if (monedaSeleccionada == "BCV") divisor = _tasasActuales["BCV"] > 0 ? _tasasActuales["BCV"] : 1m;
            else if (monedaSeleccionada == "EUR") divisor = _tasasActuales["EUR"] > 0 ? _tasasActuales["EUR"] : 1m;
            else if (monedaSeleccionada == "USDT") divisor = _tasasActuales["USDT"] > 0 ? _tasasActuales["USDT"] : 1m;

            decimal ingresos = _resumenActual.TotalIngresosBS / divisor;
            decimal egresos = _resumenActual.TotalEgresosBS / divisor;
            decimal neto = _resumenActual.SaldoNetoBS / divisor;

            txtIngresos.Text = $"{ingresos:N2} {monedaSeleccionada}";
            txtEgresos.Text = $"{egresos:N2} {monedaSeleccionada}";
            txtSaldoNeto.Text = $"{neto:N2} {monedaSeleccionada}";
        }

        private void BtnRegistrarEgreso_Click(object sender, RoutedEventArgs e)
        {
            var cultura = new CultureInfo("es-VE");

            if (string.IsNullOrWhiteSpace(cmbCategoria.Text))
            {
                AlertaWindow.MostrarAdvertencia("Debe seleccionar o escribir una Categoría.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtConcepto.Text))
            {
                AlertaWindow.MostrarAdvertencia("Debe describir el motivo del gasto en el Detalle.");
                return;
            }

            if (!decimal.TryParse(txtMonto.Text, NumberStyles.Any, cultura, out decimal monto) || monto <= 0)
            {
                AlertaWindow.MostrarAdvertencia("Ingrese un monto válido mayor a cero (Ej: 50,00).");
                return;
            }

            string moneda = ((ComboBoxItem)cmbMoneda.SelectedItem).Content.ToString()!;
            string categoria = cmbCategoria.Text.Trim();
            string concepto = txtConcepto.Text.Trim();

            if (AlertaWindow.SolicitarConfirmacion($"¿Está seguro de extraer {monto:N2} {moneda} de la caja para '{categoria}'?"))
            {
                try
                {
                    _movimientoService.RegistrarEgreso(monto, moneda, categoria, concepto);
                    AlertaWindow.MostrarExito("Gasto registrado y descontado de la caja exitosamente.");

                    txtMonto.Clear();
                    txtConcepto.Clear();
                    CargarDatos(); // Recargar gráficas con los nuevos montos
                }
                catch (Exception ex)
                {
                    AlertaWindow.MostrarError(ex.Message);
                }
            }
        }
    }
}