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
        public string MonedaUI { get; set; } = string.Empty; // Muestra "$" o "BS"

        // Traduce "$" a "BCV" para la Base de Datos internamente
        public string MonedaBD => MonedaUI == "$" ? "BCV" : "BS";
        public string MontoStr => $"{Monto:N2} {MonedaUI}";
    }

    public partial class MainWindow : Window
    {
        private readonly PagoService _pagoService;
        private readonly CajaService _cajaService;
        private readonly DevolucionService _devolucionService;
        private readonly TasaCambioService _tasaService;
        private readonly InscripcionService _inscripcionService;
        private readonly MovimientoCajaService _movimientoService;

        private Dictionary<string, decimal> _tasas = new Dictionary<string, decimal>();
        private ObservableCollection<DetallePagoUI> _listaDetalles = new ObservableCollection<DetallePagoUI>();

        public MainWindow()
        {
            InitializeComponent();
            _pagoService = new PagoService();
            _cajaService = new CajaService();
            _devolucionService = new DevolucionService();
            _tasaService = new TasaCambioService();
            _inscripcionService = new InscripcionService();
            _movimientoService = new MovimientoCajaService();

            CargarEstudiantes();
            dgDetalles.ItemsSource = _listaDetalles;

            // CONTROL ESTRICTO DE ROLES
            string rolActual = SesionService.Instancia.UsuarioActual?.Rol ?? "Cajero";

            if (rolActual == "Admin" || rolActual == "Programador")
            {
                // Permisos de Administrador
                if (FindName("btnConfigurarTasa") is Button btnTasa) btnTasa.Visibility = Visibility.Visible;
                if (FindName("btnGestionUsuarios") is Button btnUsr) btnUsr.Visibility = Visibility.Visible;
                if (FindName("btnAuditoria") is Button btnAud) btnAud.Visibility = Visibility.Visible;
                if (FindName("btnCursos") is Button btnCursos) btnCursos.Visibility = Visibility.Visible;
                if (FindName("btnMantenimiento") is Button btnMto) btnMto.Visibility = Visibility.Visible;
            }

            if (rolActual == "Programador")
            {
                // Permisos Únicos de Programador
                if (FindName("btnLicencia") is Button btnLic) btnLic.Visibility = Visibility.Visible;
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
                txtTasaDia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
            }
            else
            {
                txtTasaDia.Text = $"Tasa $: {_tasas["BCV"]:N2} Bs";
                txtTasaDia.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"));
            }
        }

        private void BtnConfigurarTasa_Click(object sender, RoutedEventArgs e)
        {
            if (_tasas["BCV"] > 0)
            {
                AlertaWindow.MostrarInfo("Las tasas de hoy ya fueron configuradas.\nPor seguridad, no se pueden alterar a mitad del día.");
                return;
            }
            new ConfigurarTasaWindow { Owner = this }.ShowDialog();
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
            var caja = _cajaService.ObtenerCajaAbierta();
            if (caja != null)
                txtRecaudadoHoy.Text = $"Bs. {_pagoService.ObtenerTotalHoy(caja.FechaApertura):N2}"; // Solo lo de hoy

            var resumen = _movimientoService.CalcularResumenGlobal();
            txtTotalHoy.Text = $"Bs. {resumen.SaldoNetoBS:N2}"; // El Banco real
        }

        private void ActualizarHistorial()
        {
            string busqueda = "";
            if (FindName("txtBuscarPago") is TextBox txtBuscador) busqueda = txtBuscador.Text.Trim();
            dgHistorial.ItemsSource = _pagoService.ObtenerPagos(100, busqueda);
        }

        private void TxtBuscarPago_TextChanged(object sender, TextChangedEventArgs e) => ActualizarHistorial();

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
            if (ventanaCierre.CajaCerradaExitosamente) ValidarEstadoCaja();
        }

        private void BtnFlujoCaja_Click(object sender, RoutedEventArgs e)
        {
            if (_cajaService.ObtenerCajaAbierta() == null)
            {
                AlertaWindow.MostrarAdvertencia("Debe Abrir la Caja primero para poder gestionar el flujo de dinero.");
                return;
            }
            new FlujoCajaWindow { Owner = this }.ShowDialog();
            ValidarEstadoCaja();
        }

        // --- MOTOR DE CONVERSIÓN Y PAGOS MIXTOS ---

        private decimal ConvertirMoneda(decimal monto, string monedaOrigen, string monedaDestino)
        {
            if (monedaOrigen == monedaDestino) return monto;
            if (_tasas["BCV"] <= 0) return 0;

            decimal montoEnBs = monto * _tasas[monedaOrigen];
            return montoEnBs / _tasas[monedaDestino];
        }

        private void CalcularTotalRecibo()
        {
            decimal sumaConvertida = 0m;
            foreach (var detalle in _listaDetalles)
            {
                // Todos los pagos se suman convertidos a la moneda BASE de presentación (BS)
                sumaConvertida += ConvertirMoneda(detalle.Monto, detalle.MonedaBD, "BS");
            }
            txtSumaTotal.Text = $"{sumaConvertida:N2} BS";
        }

        // AUTO-SELECCIONAR MONEDA SEGÚN MÉTODO DE PAGO
        private void CmbMetodoPago_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbMonedaDetalle == null || cmbMetodoPago.SelectedItem == null) return;

            var seleccionado = ((ComboBoxItem)cmbMetodoPago.SelectedItem).Content.ToString();

            // Si es Divisas (Físico) o Zelle lo forzamos a "$"
            if (seleccionado == "Divisas (Físico)" || seleccionado == "Zelle")
                cmbMonedaDetalle.SelectedIndex = 1; // 1 = "$"
            else
                cmbMonedaDetalle.SelectedIndex = 0; // 0 = "BS"
        }

        private void BtnAgregarDetalle_Click(object sender, RoutedEventArgs e)
        {
            if (_tasas["BCV"] <= 0)
            {
                AlertaWindow.MostrarError("Debe configurar la tasa de cambio del dólar antes de empezar a cobrar.");
                return;
            }

            var formatoEstricto = new NumberFormatInfo { NumberDecimalSeparator = ",", NumberGroupSeparator = "." };

            if (!decimal.TryParse(txtMontoDetalle.Text.Trim(), NumberStyles.Number, formatoEstricto, out decimal monto) || monto <= 0)
            {
                AlertaWindow.MostrarAdvertencia("Ingrese un monto numérico válido mayor a cero (Ej. 1.250,50).");
                return;
            }

            var comboMetodo = (ComboBoxItem)cmbMetodoPago.SelectedItem;
            string monedaUI = ((ComboBoxItem)cmbMonedaDetalle.SelectedItem).Content.ToString()!;

            _listaDetalles.Add(new DetallePagoUI
            {
                MetodoId = int.Parse(comboMetodo.Tag.ToString()!),
                MetodoNombre = comboMetodo.Content.ToString()!,
                Monto = monto,
                MonedaUI = monedaUI,
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

        private void CargarEstudiantes() => cmbEstudiante.ItemsSource = _inscripcionService.ObtenerInscripcionesActivas();

        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            if (!(cmbEstudiante.SelectedItem is InscripcionInfo estudianteSeleccionado))
            {
                AlertaWindow.MostrarAdvertencia("Debe buscar y seleccionar un estudiante de la lista.");
                return;
            }

            if (_listaDetalles.Count == 0)
            {
                AlertaWindow.MostrarAdvertencia("Debe añadir al menos un método de pago a la factura.");
                return;
            }

            if (_tasas["BCV"] <= 0)
            {
                AlertaWindow.MostrarError("Falta la tasa del día.");
                return;
            }

            string cedula = estudianteSeleccionado.Cedula;
            string nombre = estudianteSeleccionado.NombreCompleto;
            int inscripcionId = estudianteSeleccionado.InscripcionId;
            int conceptoId = int.Parse(((ComboBoxItem)cmbConcepto.SelectedItem).Tag.ToString()!);

            // Atrapamos la Nota
            string nota = "";
            if (FindName("txtNotaPago") is TextBox txtNota) nota = txtNota.Text.Trim();

            // INTELIGENCIA DE MONEDA BASE: Si TODO lo que pagó fue en Dólares, el recibo sale en Dólares. Si hay Bolívares mezclados, el recibo base sale en BS.
            string monedaFacturaBD = _listaDetalles.All(d => d.MonedaBD == "BCV") ? "BCV" : "BS";
            string monedaFacturaUI = monedaFacturaBD == "BCV" ? "$" : "BS";

            // GENERADOR DE NÚMEROS SECUENCIALES (Ej: REC-20261108-00001)
            string prefijo = $"REC-{DateTime.Now:yyyyMMdd}-";
            string? ultimoRecibo = _pagoService.ObtenerUltimoReciboDelDia(prefijo);
            int secuencial = 1;
            if (!string.IsNullOrEmpty(ultimoRecibo))
            {
                string numStr = ultimoRecibo.Substring(prefijo.Length);
                if (int.TryParse(numStr, out int num)) secuencial = num + 1;
            }
            string numeroRecibo = $"{prefijo}{secuencial:D5}";

            decimal totalFacturaConvertida = 0m;
            decimal totalFacturaEnUSD = 0m; // CALCULO EXACTO DE MOROSIDAD EN DÓLARES

            var listaBd = new List<PagoDetalle>();
            foreach (var d in _listaDetalles)
            {
                totalFacturaConvertida += ConvertirMoneda(d.Monto, d.MonedaBD, monedaFacturaBD);
                totalFacturaEnUSD += ConvertirMoneda(d.Monto, d.MonedaBD, "BCV");

                decimal montoEnBs = d.Monto * _tasas[d.MonedaBD];
                listaBd.Add(new PagoDetalle(0, d.MetodoId, d.Monto, d.MonedaBD, _tasas[d.MonedaBD], montoEnBs, d.Referencia) { MetodoNombre = d.MetodoNombre });
            }

            btnRegistrar.IsEnabled = false;

            try
            {
                // Se envía la nota al repositorio (Asegúrate de que PagoService acepte este parámetro final)
                _pagoService.RegistrarPago(numeroRecibo, cedula, inscripcionId, conceptoId, totalFacturaConvertida, totalFacturaEnUSD, monedaFacturaBD, DateTime.Now.AddDays(30), listaBd, nota);

                ValidarEstadoCaja();
                ActualizarHistorial();

                // Recuperamos el nombre del método para imprimirlo visualmente
                foreach (var d in listaBd) d.MetodoNombre = _listaDetalles.First(x => x.MetodoId == d.MetodoPagoId).MetodoNombre;

                // Si la factura salió en BCV, la mandamos como "$" a la impresora
                foreach (var d in listaBd) if (d.MonedaCodigo == "BCV") d.MonedaCodigo = "$";

                var pagoTemp = new Pago(numeroRecibo, cedula, inscripcionId, conceptoId, totalFacturaConvertida, monedaFacturaUI, SesionService.Instancia.UsuarioActual?.Username ?? "Cajero", DateTime.Now)
                { FechaPago = DateTime.Now, Observaciones = nota };

                ImpresorRecibo.Imprimir(pagoTemp, nombre, ((ComboBoxItem)cmbConcepto.SelectedItem).Content.ToString()!, listaBd);

                AlertaWindow.MostrarExito("Factura procesada con éxito.");

                cmbEstudiante.SelectedIndex = -1;
                if (FindName("txtNotaPago") is TextBox t) t.Clear();
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

        private void BtnNuevoEstudiante_Click(object sender, RoutedEventArgs e)
        {
            new EstudiantesWindow { Owner = this }.ShowDialog();
            CargarEstudiantes();
        }

        // --- ACCIONES EN EL HISTORIAL (DATA GRID) ---

        private void BtnImprimir_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Pago pagoSeleccionado)
            {
                var detalles = _pagoService.ObtenerDetalles(pagoSeleccionado.Id);
                // Convertimos el BCV en $ para que en el recibo se vea bien
                foreach (var d in detalles) if (d.MonedaCodigo == "BCV") d.MonedaCodigo = "$";

                ImpresorRecibo.Imprimir(pagoSeleccionado, pagoSeleccionado.NombreEstudiante, "Concepto ID: " + pagoSeleccionado.ConceptoId, detalles);
            }
        }

        private void BtnDevolver_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Pago pagoSeleccionado)
            {
                if (pagoSeleccionado.Estado == "Devuelto") { AlertaWindow.MostrarInfo("Este recibo ya ha sido devuelto anteriormente."); return; }

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
                    catch (Exception ex) { AlertaWindow.MostrarError(ex.Message); }
                }
            }
        }

        // --- NAVEGACIÓN Y SESIÓN (OTROS MÓDULOS) ---

        private void BtnMorosos_Click(object sender, RoutedEventArgs e) => new MorosidadWindow { Owner = this }.ShowDialog();
        private void BtnCalendario_Click(object sender, RoutedEventArgs e) => new CalendarioWindow { Owner = this }.ShowDialog();
        private void BtnCursos_Click(object sender, RoutedEventArgs e) => new CursosWindow { Owner = this }.ShowDialog();
        private void BtnGestionUsuarios_Click(object sender, RoutedEventArgs e) => new GestionUsuariosWindow().ShowDialog();
        private void BtnAuditoria_Click(object sender, RoutedEventArgs e) => new AuditoriaWindow().ShowDialog();
        private void BtnMantenimiento_Click(object sender, RoutedEventArgs e) => new MantenimientoWindow { Owner = this }.ShowDialog();
        private void BtnLicencia_Click(object sender, RoutedEventArgs e) => new LicenciaWindow { Owner = this }.ShowDialog();

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            if (AlertaWindow.SolicitarConfirmacion("¿Está seguro de que desea cerrar la sesión actual?", "Cerrar Sesión"))
            {
                SesionService.Instancia.CerrarSesion();
                new LoginWindow().Show();
                this.Close();
            }
        }
    }
}