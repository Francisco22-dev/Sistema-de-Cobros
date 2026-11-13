using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    // Modelo auxiliar para desplegar grupos de forma amigable
    public class GrupoComboItem
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public string Display => $"[{Codigo}] {Nombre} (Inicia: {FechaInicio:dd/MM/yyyy})";
    }

    public partial class EstudiantesWindow : Window
    {
        private readonly EstudianteService _estudianteService = new EstudianteService();
        private readonly InscripcionService _inscripcionService = new InscripcionService();
        private readonly GrupoService _grupoService = new GrupoService();
        private readonly CursoService _cursoService = new CursoService();

        private InscripcionInfo? _seleccionada;
        private List<Curso> _cursosActivos = new List<Curso>();

        public EstudiantesWindow()
        {
            InitializeComponent();
            CargarCursosEnCascada();
            CargarInscripciones();
        }

        private void CargarCursosEnCascada()
        {
            try
            {
                _cursosActivos = _cursoService.ObtenerCursos().Where(c => c.Activo).ToList();
                cmbNuevoCurso.ItemsSource = _cursosActivos;
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error cargando cursos: {ex.Message}");
            }
        }

        private void CmbNuevoCurso_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbNuevoCurso.SelectedItem is Curso cursoSeleccionado)
            {
                try
                {
                    // Cargar únicamente los grupos del curso seleccionado
                    var grupos = _grupoService.ObtenerPorCurso(cursoSeleccionado.Codigo)
                        .Where(g => g.Activo && g.Estado != "Cancelado")
                        .Select(g => new GrupoComboItem
                        {
                            Id = g.Id,
                            Codigo = g.Codigo,
                            Nombre = string.IsNullOrWhiteSpace(g.Nombre) ? "Sin Horario" : g.Nombre,
                            FechaInicio = g.FechaInicio
                        }).ToList();

                    cmbNuevoGrupo.ItemsSource = grupos;
                    cmbNuevoGrupo.IsEnabled = grupos.Count > 0;
                    if (grupos.Count > 0) cmbNuevoGrupo.SelectedIndex = 0;

                    // Ficha informativa dinámica
                    pnlInfoCurso.Visibility = Visibility.Visible;
                    txtInfoCurso.Text = $"Curso: {cursoSeleccionado.Nombre} • Duración: {cursoSeleccionado.DuracionSemanas / 4} meses ({cursoSeleccionado.DuracionSemanas} clases) • Precio por clase: {cursoSeleccionado.PrecioMensualidad:N2} $ • Inscripción: {cursoSeleccionado.PrecioInscripcion:N2} $";
                }
                catch (Exception ex)
                {
                    AlertaWindow.MostrarError(ex.Message);
                }
            }
            else
            {
                cmbNuevoGrupo.ItemsSource = null;
                cmbNuevoGrupo.IsEnabled = false;
                pnlInfoCurso.Visibility = Visibility.Collapsed;
            }
        }

        private void CmbNuevoGrupo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Mantiene el estado sincronizado
        }

        private void CargarInscripciones()
        {
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
                txtAlumnoSeleccionado.Text = $"Alumno: {info.NombreCompleto} (C.I. {info.Cedula}) - Cohorte: [{info.GrupoCodigo}] - Estado: {info.Estado}";
                txtMotivo.Text = info.Observaciones ?? "";
            }
            else
            {
                _seleccionada = null;
                panelAcciones.IsEnabled = false;
                txtAlumnoSeleccionado.Text = "Seleccione un estudiante de la tabla superior para gestionar su estado o cambiarlo de grupo.";
                txtMotivo.Clear();
            }
        }

        // ========================================================
        // TRASLADO DE GRUPO DENTRO DEL MISMO CURSO
        // ========================================================

        private void BtnAbrirModalCambio_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionada == null) return;

            txtModalAlumno.Text = $"Estudiante: {_seleccionada.NombreCompleto} (C.I. {_seleccionada.Cedula})";
            txtModalCurso.Text = $"Curso: {_seleccionada.CursoNombre} (Código: {_seleccionada.CursoCodigo})";
            txtModalGrupoActual.Text = $"Horario Actual: [{_seleccionada.GrupoCodigo}] {_seleccionada.GrupoNombre}";
            txtModalMotivo.Clear();

            // Cargar los otros grupos disponibles del MISMO curso
            var otrosGrupos = _grupoService.ObtenerPorCurso(_seleccionada.CursoCodigo)
                .Where(g => g.Id != _seleccionada.GrupoId && g.Activo && g.Estado != "Cancelado")
                .Select(g => new GrupoComboItem
                {
                    Id = g.Id,
                    Codigo = g.Codigo,
                    Nombre = string.IsNullOrWhiteSpace(g.Nombre) ? "Sin Horario" : g.Nombre,
                    FechaInicio = g.FechaInicio
                }).ToList();

            if (otrosGrupos.Count == 0)
            {
                AlertaWindow.MostrarAdvertencia($"No existen otros horarios activos registrados para el curso '{_seleccionada.CursoNombre}'. Primero cree otro grupo en la ventana Cursos.");
                return;
            }

            cmbModalNuevosGrupos.ItemsSource = otrosGrupos;
            cmbModalNuevosGrupos.SelectedIndex = 0;

            modalCambioGrupo.Visibility = Visibility.Visible;
        }

        private void BtnCerrarModalCambio_Click(object sender, RoutedEventArgs e)
        {
            modalCambioGrupo.Visibility = Visibility.Collapsed;
        }

        private void BtnConfirmarCambioGrupo_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionada == null || cmbModalNuevosGrupos.SelectedItem is not GrupoComboItem nuevoGrupo) return;

            // Regla 2: Validar si tiene deuda y pedir confirmación explícita
            bool tieneDeuda = _inscripcionService.TieneDeudaPendiente(_seleccionada.Cedula);
            if (tieneDeuda)
            {
                bool continuar = AlertaWindow.SolicitarConfirmacion(
                    $"El estudiante presenta semanas pendientes de pago en su horario actual.\n\n¿Desea cambiarlo de grupo de todas formas trasladando su historial de pagos al nuevo calendario?",
                    "Estudiante con Deuda");

                if (!continuar) return;
            }
            else
            {
                bool confirmar = AlertaWindow.SolicitarConfirmacion(
                    $"¿Está seguro de trasladar a '{_seleccionada.NombreCompleto}' al grupo [{nuevoGrupo.Codigo}] {nuevoGrupo.Nombre}?",
                    "Confirmar Traslado");

                if (!confirmar) return;
            }

            try
            {
                _inscripcionService.TrasladarEstudianteDeGrupo(
                    _seleccionada.InscripcionId,
                    nuevoGrupo.Id,
                    _seleccionada.GrupoCodigo,
                    nuevoGrupo.Codigo,
                    _seleccionada.Observaciones ?? "",
                    txtModalMotivo.Text);

                AlertaWindow.MostrarExito($"Estudiante trasladado exitosamente al grupo [{nuevoGrupo.Codigo}]. Sus pagos históricos han sido conservados.");

                modalCambioGrupo.Visibility = Visibility.Collapsed;
                CargarInscripciones();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al trasladar: {ex.Message}");
            }
        }

        // ========================================================
        // GESTIÓN DE ESTADOS (DESERTORES / SUSPENSIONES)
        // ========================================================

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

            if (string.IsNullOrWhiteSpace(cedula) || string.IsNullOrWhiteSpace(nombre) || cmbNuevoGrupo.SelectedValue == null)
            {
                AlertaWindow.MostrarAdvertencia("Cédula, Nombre, Curso y Horario son obligatorios.");
                return;
            }

            int grupoId = (int)cmbNuevoGrupo.SelectedValue;

            try
            {
                _estudianteService.CrearEstudiante(cedula, nombre, telefono, email, null);
                _inscripcionService.RegistrarInscripcion(cedula, grupoId, "Inscripción en sistema");

                AlertaWindow.MostrarExito("Estudiante registrado e inscrito exitosamente.");
                txtCedula.Clear();
                txtNombre.Clear();
                txtTelefono.Clear();
                txtEmail.Clear();
                cmbNuevoCurso.SelectedIndex = -1;
                CargarInscripciones();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al registrar: {ex.Message}");
            }
        }
    }
}