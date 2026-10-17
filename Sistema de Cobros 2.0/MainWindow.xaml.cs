using CapaNegocio;
using Entidades;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            CargarEstudiantes();

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

            if (_tasas["BCV"] <= 0)
            {
                txtTasaDia.Text = "TASAS NO CONFIGURADAS";
                txtTasaDia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")); // Rojo
            }
            else
            {
                txtTasaDia.Text = $"BCV: {_tasas["BCV"]:N2} | EUR: {_tasas["EUR"]:N2} | USDT: {_tasas["USDT"]:N2}";
                txtTasaDia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2")); // Azul
            }
        }

        private void BtnConfigurarTasa_Click(object sender, RoutedEventArgs e)
        {
            if (_tasas["BCV"] > 0)
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
            var ventanaCierre = new CierreCajaWindow { Owner = this };
            ventanaCierre.ShowDialog();

            if (ventanaCierre.CajaCerradaExitosamente)
            {
                ValidarEstadoCaja();
            }
        }

        // --- MOTOR DE CONVERSIÓN Y PAGOS MIXTOS ---

        private decimal ConvertirMoneda(decimal monto, string monedaOrigen, string monedaDestino)
        {
            if (monedaOrigen == monedaDestino) return monto;
            if (_tasas["BCV"] <= 0) return 0; // Previene fallos si no hay tasas configuradas

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
            if (_tasas["BCV"] <= 0)
            {
                AlertaWindow.MostrarError("Debe configurar las tasas de cambio del día antes de empezar a cobrar.");
                return;
            }

            // 1. FORMATO A PRUEBA DE BALAS
            var formatoEstricto = new System.Globalization.NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = "."
            };

            // 2. Usamos NumberStyles.Number para que acepte tanto puntos como comas
            if (!decimal.TryParse(txtMontoDetalle.Text.Trim(), System.Globalization.NumberStyles.Number, formatoEstricto, out decimal monto) || monto <= 0)
            {
                AlertaWindow.MostrarAdvertencia("Ingrese un monto numérico válido mayor a cero (Ej. 1.250,50).");
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

        // (En la parte de arriba, añade este servicio a los declarados)
        private readonly InscripcionService _inscripcionService = new InscripcionService();

        // Llama a este método en el constructor public MainWindow()
        private void CargarEstudiantes()
        {
            cmbEstudiante.ItemsSource = _inscripcionService.ObtenerInscripcionesActivas();
        }

        // --- ARREGLO DEL PARSEO DE NÚMEROS ---
        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            // OBTENER EL ESTUDIANTE REAL SELECCIONADO
            if (!(cmbEstudiante.SelectedItem is InscripcionInfo estudianteSeleccionado))
            {
                AlertaWindow.MostrarAdvertencia("Debe buscar y seleccionar un estudiante de la lista.");
                return;
            }

            string cedula = estudianteSeleccionado.Cedula;
            string nombre = estudianteSeleccionado.NombreCompleto;
            int inscripcionId = estudianteSeleccionado.InscripcionId;

            string monedaFactura = ((ComboBoxItem)cmbMonedaRecibo.SelectedItem).Content.ToString()!;

            if (_listaDetalles.Count == 0)
            {
                AlertaWindow.MostrarAdvertencia("Debe añadir al menos un método de pago a la factura.");
                return;
            }

            string numeroRecibo = "REC-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            int conceptoId = int.Parse(((ComboBoxItem)cmbConcepto.SelectedItem).Tag.ToString()!);
            decimal totalFacturaConvertida = 0m;

            var listaBd = new List<PagoDetalle>();
            foreach (var d in _listaDetalles)
            {
                totalFacturaConvertida += ConvertirMoneda(d.Monto, d.Moneda, monedaFactura);
                decimal montoEnBs = d.Monto * _tasas[d.Moneda];
                listaBd.Add(new PagoDetalle(0, d.MetodoId, d.Monto, d.Moneda, _tasas[d.Moneda], montoEnBs, d.Referencia) { MetodoNombre = d.MetodoNombre });
            }

            btnRegistrar.IsEnabled = false;

            try
            {
                // AHORA PASAMOS EL INSCRIPCION_ID REAL
                _pagoService.RegistrarPago(numeroRecibo, cedula, inscripcionId, conceptoId, totalFacturaConvertida, monedaFactura, DateTime.Now.AddDays(30), listaBd);

                ValidarEstadoCaja();
                ActualizarHistorial();

                if (AlertaWindow.SolicitarConfirmacion("Pago registrado exitosamente.\n\n¿Desea imprimir el recibo?", "Imprimir Factura"))
                {
                    var pagoTemp = new Pago(numeroRecibo, cedula, inscripcionId, conceptoId, totalFacturaConvertida, monedaFactura, SesionService.Instancia.UsuarioActual?.Username ?? "Cajero", DateTime.Now) { FechaPago = DateTime.Now };
                    ImpresorRecibo.Imprimir(pagoTemp, nombre, ((ComboBoxItem)cmbConcepto.SelectedItem).Content.ToString()!, listaBd);
                }

                cmbEstudiante.SelectedIndex = -1; // Limpiar buscador
                _listaDetalles.Clear();
                CalcularTotalRecibo();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al registrar el pago: {ex.Message}");
            }
            finally
            {
                btnRegistrar.IsEnabled = _cajaService.ObtenerCajaAbierta() != null;
            }
        }

        // Evento para abrir la ventana de estudiantes
        private void BtnNuevoEstudiante_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new EstudiantesWindow { Owner = this };
            ventana.ShowDialog();
            CargarEstudiantes(); // Recargar el combobox por si se creó uno nuevo
        }

        // --- ACCIONES EN EL HISTORIAL (DATA GRID) ---

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Pago pagoSeleccionado)
            {
                // Ahora SÍ cargamos el detalle real desde la Base de Datos
                var detalles = _pagoService.ObtenerDetalles(pagoSeleccionado.Id);
                ImpresorRecibo.Imprimir(pagoSeleccionado, "Estudiante (Ver Cédula)", "Concepto ID: " + pagoSeleccionado.ConceptoId, detalles);
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