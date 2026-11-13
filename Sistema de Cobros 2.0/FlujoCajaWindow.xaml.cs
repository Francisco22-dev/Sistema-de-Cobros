using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public partial class FlujoCajaWindow : Window
    {
        private readonly MovimientoCajaService _movimientoService = new MovimientoCajaService();
        private readonly TasaCambioService _tasaService = new TasaCambioService();

        private ResumenFlujo _resumenActual = new ResumenFlujo();
        private List<MovimientoCaja> _todosLosEgresos = new List<MovimientoCaja>();

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

                _resumenActual = _movimientoService.CalcularResumenGlobal();
                ActualizarTarjetasVisuales();

                _todosLosEgresos = _movimientoService.ObtenerTodosLosEgresos();
                FiltrarEgresos();

                // Desglose de gaveta física y cuentas bancarias
                txtDesglose.Text = $"Efectivo: {_resumenActual.FisicoBs:N2} Bs | Banco: {_resumenActual.BancoBs:N2} Bs\nDivisas: {_resumenActual.FisicoUsd:N2} $ | Zelle: {_resumenActual.BancoUsd:N2} $";
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
            if (cmbMonedaDashboard.SelectedItem is not ComboBoxItem item) return;
            string monedaSeleccionada = item.Content.ToString()!;

            // Si el usuario elige ver en Dólares ($)
            if (monedaSeleccionada.Contains("$") || monedaSeleccionada.Contains("BCV"))
            {
                txtIngresos.Text = $"{_resumenActual.TotalIngresosUSD:N2} $";
                txtEgresos.Text = $"{_resumenActual.TotalEgresosUSD:N2} $";
                txtSaldoNeto.Text = $"{_resumenActual.SaldoNetoUSD:N2} $";
            }
            else // Si elige ver en Bolívares (BS)
            {
                txtIngresos.Text = $"{_resumenActual.TotalIngresosBS:N2} BS";
                txtEgresos.Text = $"{_resumenActual.TotalEgresosBS:N2} BS";
                txtSaldoNeto.Text = $"{_resumenActual.SaldoNetoBS:N2} BS";
            }
        }

        // ========================================================
        // BUSCADOR DE EGRESOS OPTIMIZADO
        // ========================================================

        private void FiltrarEgresos()
        {
            string filtro = txtBuscarEgreso?.Text.Trim().ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(filtro))
            {
                dgEgresos.ItemsSource = _todosLosEgresos;
            }
            else
            {
                dgEgresos.ItemsSource = _todosLosEgresos.Where(e =>
                    (e.Concepto != null && e.Concepto.ToLower().Contains(filtro)) ||
                    (e.Categoria != null && e.Categoria.ToLower().Contains(filtro)) ||
                    (e.Metodo != null && e.Metodo.ToLower().Contains(filtro)) ||
                    (e.MonedaCodigo != null && e.MonedaCodigo.ToLower().Contains(filtro)) ||
                    e.Monto.ToString("N2").Contains(filtro)
                ).ToList();
            }
        }

        private void BtnBuscarEgreso_Click(object sender, RoutedEventArgs e) => FiltrarEgresos();

        private void TxtBuscarEgreso_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) FiltrarEgresos();
        }

        private void BtnLimpiarBuscarEgreso_Click(object sender, RoutedEventArgs e)
        {
            txtBuscarEgreso.Text = string.Empty;
            FiltrarEgresos();
        }

        // ========================================================
        // REGISTRO DE GASTO
        // ========================================================

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
            string metodo = ((ComboBoxItem)cmbMetodoEgreso.SelectedItem).Content.ToString()!;

            if (AlertaWindow.SolicitarConfirmacion($"¿Está seguro de extraer {monto:N2} {moneda} de la caja para '{categoria}'?"))
            {
                try
                {
                    _movimientoService.RegistrarEgreso(monto, moneda, categoria, concepto, metodo);
                    AlertaWindow.MostrarExito("Gasto registrado y descontado de la caja exitosamente.");

                    txtMonto.Clear();
                    txtConcepto.Clear();
                    CargarDatos();
                }
                catch (Exception ex)
                {
                    AlertaWindow.MostrarError(ex.Message);
                }
            }
        }

        private void BtnOperaciones_Click(object sender, RoutedEventArgs e)
        {
            new OperacionesCajaWindow { Owner = this }.ShowDialog();
            CargarDatos();
        }
    }
}