using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class FlujoCajaWindow : Window
    {
        private readonly MovimientoCajaService _movimientoService = new MovimientoCajaService();

        public FlujoCajaWindow()
        {
            InitializeComponent();
            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                // Cargar categorías (Las tuyas + las que inventen)
                var categorias = _movimientoService.ObtenerCategorias();
                cmbCategoria.ItemsSource = categorias;
                if (categorias.Count > 0) cmbCategoria.SelectedIndex = 0;

                // Cargar Tarjetas
                var resumen = _movimientoService.CalcularResumenCaja();
                txtIngresos.Text = $"{resumen.TotalIngresosBS:N2} BS";
                txtEgresos.Text = $"{resumen.TotalEgresosBS:N2} BS";
                txtSaldoNeto.Text = $"{resumen.SaldoNetoBS:N2} BS";

                // Cargar Tabla
                dgEgresos.ItemsSource = _movimientoService.ObtenerEgresosDeHoy();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError(ex.Message);
            }
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
                    CargarDatos(); // Recargar gráficas
                }
                catch (Exception ex)
                {
                    AlertaWindow.MostrarError(ex.Message);
                }
            }
        }
    }
}