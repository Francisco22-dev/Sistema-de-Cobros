using System;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public partial class EstudiantesWindow : Window
    {
        private readonly EstudianteService _estudianteService = new EstudianteService();
        private readonly InscripcionService _inscripcionService = new InscripcionService();
        private readonly GrupoService _grupoService = new GrupoService();

        private InscripcionInfo? _seleccionada;

        public EstudiantesWindow()
        {
            InitializeComponent();
            CargarGrupos();
            CargarInscripciones();
        }

        private void CargarGrupos()
        {
            try
            {
                var grupos = _grupoService.ObtenerGruposActivos();
                cmbGrupos.ItemsSource = grupos;
                if (grupos.Count > 0) cmbGrupos.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error cargando grupos: {ex.Message}");
            }
        }

        private void CargarInscripciones()
        {
            // PROTECCIÓN: Si la tabla aún no se ha dibujado en pantalla, no ejecutar
            if (dgInscripciones == null) return;

            try
            {
                string filtro = txtBuscar?.Text.Trim() ?? "";
                string estado = (cmbFiltroEstado?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";

                dgInscripciones.ItemsSource = _inscripcionService.ObtenerTodasLasInscripciones(filtro, estado);
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al listar inscripciones: {ex.Message}");
            }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dgInscripciones != null) CargarInscripciones();
        }

        private void CmbFiltroEstado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgInscripciones != null) CargarInscripciones();
        }

        private void DgInscripciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgInscripciones.SelectedItem is InscripcionInfo info)
            {
                _seleccionada = info;
                panelAcciones.IsEnabled = true;
                txtAlumnoSeleccionado.Text = $"Alumno: {info.NombreCompleto} (C.I. {info.Cedula}) - Estado: {info.Estado}";
                txtMotivo.Text = info.Observaciones ?? "";
            }
            else
            {
                _seleccionada = null;
                panelAcciones.IsEnabled = false;
                txtAlumnoSeleccionado.Text = "Seleccione un estudiante de la tabla superior para gestionar su estado.";
                txtMotivo.Clear();
            }
        }

        private void AplicarCambioEstado(string nuevoEstado, string accionNombre)
        {
            if (_seleccionada == null) return;

            string confirmacion = $"¿Está seguro de marcar la inscripción de '{_seleccionada.NombreCompleto}' como {nuevoEstado.ToUpper()}?";
            if (!AlertaWindow.SolicitarConfirmacion(confirmacion, $"Confirmar {accionNombre}"))
                return;

            try
            {
                _inscripcionService.CambiarEstadoInscripcion(_seleccionada.InscripcionId, nuevoEstado, txtMotivo.Text);
                AlertaWindow.MostrarExito($"Inscripción cambiada a '{nuevoEstado}' exitosamente.");
                CargarInscripciones();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError(ex.Message);
            }
        }

        private void BtnSuspender_Click(object sender, RoutedEventArgs e) => AplicarCambioEstado("Suspendida", "Suspensión");
        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => AplicarCambioEstado("Cancelada", "Cancelación por Deserción");
        private void BtnReactivar_Click(object sender, RoutedEventArgs e) => AplicarCambioEstado("Activa", "Reactivación");

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string cedula = txtCedula.Text.Trim();
            string nombre = txtNombre.Text.Trim();
            string telefono = txtTelefono.Text.Trim();
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(cedula) || string.IsNullOrWhiteSpace(nombre) || cmbGrupos.SelectedValue == null)
            {
                AlertaWindow.MostrarAdvertencia("Cédula, Nombre y Horario son obligatorios.");
                return;
            }

            int grupoId = (int)cmbGrupos.SelectedValue;

            try
            {
                _estudianteService.CrearEstudiante(cedula, nombre, telefono, email, null);
                _inscripcionService.RegistrarInscripcion(cedula, grupoId, "Inscripción en sistema");

                AlertaWindow.MostrarExito("Estudiante registrado e inscrito exitosamente.");
                txtCedula.Clear();
                txtNombre.Clear();
                txtTelefono.Clear();
                txtEmail.Clear();
                CargarInscripciones();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al registrar: {ex.Message}");
            }
        }
    }
}