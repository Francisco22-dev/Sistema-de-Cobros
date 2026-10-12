using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PagoService _pagoService;

        public MainWindow()
        {
            InitializeComponent();
            _pagoService = new PagoService();
            dgHistorial.ItemsSource = _pagoService.ObtenerPagos();

            // Mostrar botones de administración solo si es administrador o programador
            if (SesionService.Instancia.EsAdministrador())
            {
                btnGestionUsuarios.Visibility = Visibility.Visible;
                btnAuditoria.Visibility = Visibility.Visible;
            }
        }

        private void BtnGestionUsuarios_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new GestionUsuariosWindow();
            ventana.ShowDialog();
        }

        private void BtnAuditoria_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new AuditoriaWindow();
            ventana.ShowDialog();
        }

        private void BtnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            // Validar nombre
            if (string.IsNullOrWhiteSpace(txtNombreEstudiante.Text))
            {
                MessageBox.Show("Ingrese el nombre del estudiante.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar monto
            if (!decimal.TryParse(txtMonto.Text, NumberStyles.Currency,
                CultureInfo.CurrentCulture, out decimal monto) || monto <= 0)
            {
                MessageBox.Show("Ingrese un monto válido mayor que cero.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Generar número de recibo único
            string numeroRecibo = "REC-" + DateTime.Now.ToString("yyyyMMddHHmmss");

            // Crear detalle de pago (un solo método por ahora)
            var detalles = new List<PagoDetalle>
            {
                new PagoDetalle(0, 1, monto, "VES", 1, monto)
            };

            // Registrar el pago a través de la capa de negocio
            _pagoService.RegistrarPago(
                numeroRecibo,
                txtNombreEstudiante.Text.Trim(),
                0, // InscripcionId - pendiente módulo de inscripciones
                1, // ConceptoId - pendiente módulo de conceptos
                monto,
                "VES",
                DateTime.Now.AddDays(30),
                detalles
            );

            ActualizarTotalHoy();
            LimpiarFormulario();
        }

        private void ActualizarTotalHoy()
        {
            decimal total = _pagoService.ObtenerTotalHoy();
            txtTotalHoy.Text = total.ToString("C", CultureInfo.CurrentCulture);
        }

        private void LimpiarFormulario()
        {
            txtNombreEstudiante.Clear();
            txtMonto.Clear();
            cmbConcepto.SelectedIndex = 0;
            cmbMetodoPago.SelectedIndex = 0;
            txtNombreEstudiante.Focus();
        }
    }
}
