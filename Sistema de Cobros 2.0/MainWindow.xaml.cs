using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    // Clase temporal para manejar la UI de la lista de pagos mixtos en el DataGrid
    public class DetallePagoUI
    {
        public int MetodoId { get; set; }
        public string MetodoNombre { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public string Referencia { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public string MontoStr => $"{Monto:N2} {Moneda}";
    }

    public partial class MainWindow : Window
    {
        private readonly PagoService _pagoService;
        private readonly CajaService _cajaService;
        private readonly DevolucionService _devolucionService;
        private readonly TasaCambioService _tasaService;

        // Diccionario de tasas del día (BS siempre será 1)
        private Dictionary<string, decimal> _tasas = new Dictionary<string, decimal>();

        // Colección reactiva para la lista de pagos mixtos en pantalla
        private ObservableCollection<DetallePagoUI> _listaDetalles = new ObservableCollection<DetallePagoUI>();

        public MainWindow()
        {
            InitializeComponent();
            _pagoService = new PagoService();
            _cajaService = new CajaService();
            _devolucionService = new DevolucionService();
            _tasaService = new TasaCambioService();

            // Vincular la lista en memoria con la tabla en pantalla
            dgDetalles.ItemsSource = _listaDetalles;

            // Habilitar botones de administración según el rol
            if (SesionService.Instancia.EsAdministrador())
            {
                btnConfigurarTasa.Visibility = Visibility.Visible;
                btnGestionUsuarios.Visibility = Visibility.Visible;
                btnAuditoria.Visibility = Visibility.Visible;
            }

            ActualizarVistaTasa();
            ValidarEstadoCaja();
            ActualizarHistorial();
        }

        // --- GESTIÓN DE TASAS DE CAMBIO ---

        private void ActualizarVistaTasa()
        {
            _tasas = _tasaService.ObtenerTasasDelDia();

            if (_tasas["USD"] <= 0)
            {
                txtTasaDia.Text = "TASAS NO CONFIGURADAS";
                txtTasaDia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")); // Rojo
            }
            else
            {
                txtTasaDia.Text = $"USD: {_tasas["USD"]:N2} | EUR: {_tasas["EUR"]:N2} | USDT: {_tasas["USDT"]:N2}";
                txtTasaDia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2")); // Azul
            }
        }

        private void BtnConfigurarTasa_Click(object sender, RoutedEventArgs e)
        {
            if (_tasas["USD"] > 0)
            {
                AlertaWindow.MostrarInfo("Las tasas de hoy ya fueron configuradas.\nPor seguridad, no se pueden alterar a mitad del día.");
                return;
            }

            var ventanaTasa = new ConfigurarTasaWindow { Owner = this };
            ventanaTasa.ShowDialog();

            ActualizarVistaTasa();
        }

        // --- GESTIÓN DE LA CAJA DIARIA ---

        private void ValidarEstadoCaja()
        {
            var caja = _cajaService.ObtenerCajaAbierta();
            if (caja == null)
            {
                txtTotalHoy.Text = "CAJA CERRADA";
                bdrEstadoCaja.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));

                btnRegistrar.IsEnabled = false;
                btnAgregarDetalle.IsEnabled = false;

                btnAbrirCaja.Visibility = Visibility.Visible;
                btnCerrarCaja.Visibility = Visibility.Collapsed;
            }
            else
            {
                ActualizarTotalHoy();
                bdrEstadoCaja.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));

                btnRegistrar.IsEnabled = true;
                btnAgregarDetalle.IsEnabled = true;

                btnAbrirCaja.Visibility = Visibility.Collapsed;
                btnCerrarCaja.Visibility = Visibility.Visible;
            }
        }

        private void ActualizarTotalHoy()
        {
            txtTotalHoy.Text = $"Bs. {_pagoService.ObtenerTotalHoy():N2}";
        }

        private void ActualizarHistorial()
        {
            dgHistorial.ItemsSource = _pagoService.ObtenerPagos(100); // Límite por rendimiento (Fase 1)
        }

        private void BtnAbrirCaja_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new AperturaCajaWindow { Owner = this };
            ventana.ShowDialog();
            if (ventana.CajaAbiertaExitosamente) ValidarEstadoCaja();
        }

        private void BtnCerrarCaja_Click(object sender, RoutedEventArgs e)
        {
            if (AlertaWindow.SolicitarConfirmacion("¿Está seguro de que desea CERRAR la caja de hoy?", "Cierre de Caja"))
            {
                try
                {
                    _cajaService.CerrarCaja();
                    AlertaWindow.MostrarExito("Caja cerrada exitosamente.");
                    ValidarEstadoCaja();
                }
                catch (Exception ex)
                {
                    AlertaWindow.MostrarError(ex.Message);
                }
            }
        }

        // --- MOTOR DE CONVERSIÓN Y PAGOS MIXTOS ---

        private decimal ConvertirMoneda(decimal monto, string monedaOrigen, string monedaDestino)
        {
            if (monedaOrigen == monedaDestino) return monto;
            if (_tasas["USD"] <= 0) return 0; // Previene fallos si no hay tasas configuradas

            // 1. Llevamos todo a Bolívares (Moneda Base Nacional)
            decimal montoEnBs = monto * _tasas[monedaOrigen];

            // 2. Convertimos a la moneda destino
            return montoEnBs / _tasas[monedaDestino];
        }

        private void CalcularTotalRecibo()
        {
            // Obtener la moneda principal de la factura
            string monedaFactura = ((ComboBoxItem)cmbMonedaRecibo.SelectedItem).Content.ToString()!;
            decimal sumaConvertida = 0m;

            foreach (var detalle in _listaDetalles)
            {
                sumaConvertida += ConvertirMoneda(detalle.Monto, detalle.Moneda, monedaFactura);
            }

            txtSumaTotal.Text = $"{sumaConvertida:N2} {monedaFactura}";
        }

        private void BtnAgregarDetalle_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtMontoDetalle.Text.Replace(".", ","), out decimal monto) || monto <= 0)
            {
                AlertaWindow.MostrarAdvertencia("Ingrese un monto numérico válido mayor a cero.");
                return;
            }

            var comboMetodo = (ComboBoxItem)cmbMetodoPago.SelectedItem;
            string monedaDetalle = ((ComboBoxItem)cmbMonedaDetalle.SelectedItem).Content.ToString()!;

            _listaDetalles.Add(new DetallePagoUI
            {
                MetodoId = int.Parse(comboMetodo.Tag.ToString()!),
                MetodoNombre = comboMetodo.Content.ToString()!,
                Monto = monto,
                Moneda = monedaDetalle,
                Referencia = txtReferenciaDetalle.Text.Trim()
            });

            txtMontoDetalle.Clear();
            txtReferenciaDetalle.Clear();
            CalcularTotalRecibo();
        }

        private void BtnEliminarDetalle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DetallePagoUI detalle)
            {
                _listaDetalles.Remove(detalle);
                CalcularTotalRecibo();
            }
        }

        // --- REGISTRO PRINCIPAL DEL PAGO (FACTURA) ---

        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            string cedula = txtCedula.Text.Trim();
            string nombre = txtNombreEstudiante.Text.Trim();
            string monedaFactura = ((ComboBoxItem)cmbMonedaRecibo.SelectedItem).Content.ToString()!;

            if (string.IsNullOrWhiteSpace(cedula) || string.IsNullOrWhiteSpace(nombre))
            {
                AlertaWindow.MostrarAdvertencia("Por favor ingrese la Cédula y el Nombre del estudiante.");
                return;
            }

            if (_listaDetalles.Count == 0)
            {
                AlertaWindow.MostrarAdvertencia("Debe añadir al menos un método de pago a la factura.");
                return;
            }

            if (_tasas["USD"] <= 0)
            {
                AlertaWindow.MostrarError("Debe configurar las tasas del día (Botón superior) antes de poder procesar pagos.");
                return;
            }

            string numeroRecibo = "REC-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            int conceptoId = int.Parse(((ComboBoxItem)cmbConcepto.SelectedItem).Tag.ToString()!);
            decimal totalFacturaConvertida = 0m;

            // Convertir la UI a Entidades reales para la Base de Datos
            var listaBd = new List<PagoDetalle>();
            foreach (var d in _listaDetalles)
            {
                totalFacturaConvertida += ConvertirMoneda(d.Monto, d.Moneda, monedaFactura);
                decimal montoEnBs = d.Monto * _tasas[d.Moneda];

                listaBd.Add(new PagoDetalle(0, d.MetodoId, d.Monto, d.Moneda, _tasas[d.Moneda], montoEnBs, d.Referencia));
            }

            // Prevención de Doble-Clic
            btnRegistrar.IsEnabled = false;

            try
            {
                // NOTA: InscripcionId está fijo en '1' hasta la Fase 7 (Gestión Estudiantes)
                _pagoService.RegistrarPago(numeroRecibo, cedula, 1, conceptoId, totalFacturaConvertida, monedaFactura, DateTime.Now.AddDays(30), listaBd);

                ValidarEstadoCaja();
                ActualizarHistorial();

                if (AlertaWindow.SolicitarConfirmacion("Pago registrado exitosamente.\n\n¿Desea imprimir el recibo?", "Imprimir Factura"))
                {
                    var pagoTemp = new Pago(numeroRecibo, cedula, 1, conceptoId, totalFacturaConvertida, monedaFactura, SesionService.Instancia.UsuarioActual?.Username ?? "Cajero", DateTime.Now)
                    { FechaPago = DateTime.Now };

                    ImpresorRecibo.Imprimir(pagoTemp, nombre, ((ComboBoxItem)cmbConcepto.SelectedItem).Content.ToString()!, "Pago Mixto");
                }

                // Limpiar formulario tras éxito
                txtCedula.Clear();
                txtNombreEstudiante.Clear();
                _listaDetalles.Clear();
                CalcularTotalRecibo();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al registrar el pago: {ex.Message}");
            }
            finally
            {
                // Habilitar de nuevo el botón solo si la caja sigue abierta
                btnRegistrar.IsEnabled = _cajaService.ObtenerCajaAbierta() != null;
            }
        }

        // --- ACCIONES EN EL HISTORIAL (DATA GRID) ---

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Pago pagoSeleccionado)
            {
                ImpresorRecibo.Imprimir(pagoSeleccionado, "Estudiante (Ver Cédula)", "Concepto ID: " + pagoSeleccionado.ConceptoId, "Múltiples / Ver Sistema");
            }
        }

        private void BtnDevolver_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Pago pagoSeleccionado)
            {
                if (pagoSeleccionado.Estado == "Devuelto")
                {
                    AlertaWindow.MostrarInfo("Este recibo ya ha sido devuelto anteriormente.");
                    return;
                }

                var ventanaMotivo = new MotivoDevolucionWindow { Owner = this };
                ventanaMotivo.ShowDialog();

                if (ventanaMotivo.Confirmado)
                {
                    try
                    {
                        _devolucionService.RealizarDevolucion(pagoSeleccionado, ventanaMotivo.Motivo);
                        AlertaWindow.MostrarExito("Devolución procesada correctamente. La caja ha sido actualizada.");

                        ActualizarHistorial();
                        ValidarEstadoCaja();
                    }
                    catch (Exception ex)
                    {
                        AlertaWindow.MostrarError(ex.Message);
                    }
                }
            }
        }

        // --- OTROS MÓDULOS ---

        private void BtnGestionUsuarios_Click(object sender, RoutedEventArgs e)
        {
            new GestionUsuariosWindow().ShowDialog();
        }

        private void BtnAuditoria_Click(object sender, RoutedEventArgs e)
        {
            new AuditoriaWindow().ShowDialog();
        }
    }
}