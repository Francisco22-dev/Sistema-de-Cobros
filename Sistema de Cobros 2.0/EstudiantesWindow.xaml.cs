using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
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
        private readonly UsuarioService _usuarioService = new UsuarioService();

        private InscripcionInfo? _seleccionada;
        private List<Curso> _cursosActivos = new List<Curso>();

        // Variables de balance para el cambio de curso
        private decimal _deudaActualOrigen = 0m;
        private decimal _totalPagadoClasesOrigen = 0m;
        private decimal _saldoAdelantadoOrigen = 0m;
        private bool _pagoMatriculaOrigen = false;
        private Curso? _cursoOrigenActual;
        private Curso? _cursoDestinoSeleccionado;

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

                    pnlInfoCurso.Visibility = Visibility.Visible;
                    txtInfoCurso.Text = $"Curso: {cursoSeleccionado.Nombre} • Duración: {cursoSeleccionado.DuracionSemanas / 4} meses ({cursoSeleccionado.DuracionSemanas} clases) • Precio clase: {cursoSeleccionado.PrecioMensualidad:N2} $ • Matrícula: {cursoSeleccionado.PrecioInscripcion:N2} $";
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

        private void CmbNuevoGrupo_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

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
                txtAlumnoSeleccionado.Text = "Seleccione un estudiante de la tabla superior para gestionar su estado o cambiarlo de curso.";
                txtMotivo.Clear();
            }
        }

        // ========================================================
        // MODAL 1: CAMBIO DE HORARIO (MISMO CURSO)
        // ========================================================

        private void BtnAbrirModalCambio_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionada == null) return;

            txtModalAlumno.Text = $"Estudiante: {_seleccionada.NombreCompleto} (C.I. {_seleccionada.Cedula})";
            txtModalCurso.Text = $"Curso: {_seleccionada.CursoNombre} (Código: {_seleccionada.CursoCodigo})";
            txtModalGrupoActual.Text = $"Horario Actual: [{_seleccionada.GrupoCodigo}] {_seleccionada.GrupoNombre}";
            txtModalMotivo.Clear();

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
                AlertaWindow.MostrarAdvertencia($"No existen otros horarios activos para '{_seleccionada.CursoNombre}'.");
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

            bool tieneDeuda = _inscripcionService.TieneDeudaPendiente(_seleccionada.Cedula);
            if (tieneDeuda)
            {
                bool continuar = AlertaWindow.SolicitarConfirmacion(
                    $"El estudiante presenta semanas pendientes de pago.\n¿Desea cambiarlo de grupo de todas formas trasladando su saldo?",
                    "Estudiante con Deuda");
                if (!continuar) return;
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

                AlertaWindow.MostrarExito($"Estudiante trasladado al grupo [{nuevoGrupo.Codigo}]. Pagos conservados.");
                modalCambioGrupo.Visibility = Visibility.Collapsed;
                CargarInscripciones();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError(ex.Message);
            }
        }

        // ========================================================
        // MODAL 2: CAMBIO DE CURSO / CARRERA (COMPLETO)
        // ========================================================

        private void BtnAbrirModalCambioCurso_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionada == null) return;

            _cursoOrigenActual = _cursosActivos.FirstOrDefault(c => c.Codigo == _seleccionada.CursoCodigo);
            decimal precioClaseOrigen = _cursoOrigenActual?.PrecioMensualidad ?? 14m;

            // Obtener balance financiero actual del estudiante en este curso
            var balance = _inscripcionService.ObtenerBalanceEstudianteEnCurso(_seleccionada.InscripcionId, _seleccionada.GrupoId, precioClaseOrigen);
            _totalPagadoClasesOrigen = balance.TotalPagadoClasesUSD;
            _deudaActualOrigen = balance.DeudaUSD;
            _saldoAdelantadoOrigen = balance.SaldoAdelantadoUSD;
            _pagoMatriculaOrigen = balance.PagoMatricula;

            txtCursoModalAlumno.Text = $"Estudiante: {_seleccionada.NombreCompleto} (C.I. {_seleccionada.Cedula})";
            txtCursoModalOrigen.Text = $"Curso que abandona: {_seleccionada.CursoNombre} (Grupo: [{_seleccionada.GrupoCodigo}])";
            txtCursoModalPagado.Text = $"Total pagado en clases: {_totalPagadoClasesOrigen:N2} $ • Saldo adelantado: {_saldoAdelantadoOrigen:N2} $";

            // Alerta visual de deuda
            if (_deudaActualOrigen > 0)
            {
                bdrAlertaDeuda.Visibility = Visibility.Visible;
                txtAlertaDeudaDetalle.Text = $"Presenta una deuda pendiente de {_deudaActualOrigen:N2} $ ({balance.ClasesVistas} clases vistas).";
                chkPerdonarDeuda.IsChecked = false;
            }
            else
            {
                bdrAlertaDeuda.Visibility = Visibility.Collapsed;
                chkPerdonarDeuda.IsChecked = false;
            }

            // Exigir credenciales de admin si el usuario actual es Cajero
            bool esAdmin = SesionService.Instancia.EsAdministrador();
            bdrAutorizacionAdmin.Visibility = esAdmin ? Visibility.Collapsed : Visibility.Visible;
            txtAdminUser.Clear();
            txtAdminPass.Clear();
            txtCursoModalMotivo.Clear();

            // Cargar cursos diferentes al actual
            var otrosCursos = _cursosActivos.Where(c => c.Codigo != _seleccionada.CursoCodigo).ToList();
            cmbCursoModalNuevoCurso.ItemsSource = otrosCursos;
            cmbCursoModalNuevoGrupo.ItemsSource = null;
            cmbCursoModalNuevoGrupo.IsEnabled = false;

            chkTransferirTodoCredito.IsChecked = true;
            chkConvalidarInscripcion.IsChecked = true;
            ActualizarTextosComparativos();

            modalCambioCurso.Visibility = Visibility.Visible;
        }

        private void CmbCursoModalNuevoCurso_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCursoModalNuevoCurso.SelectedItem is Curso cursoNuevo)
            {
                _cursoDestinoSeleccionado = cursoNuevo;
                var grupos = _grupoService.ObtenerPorCurso(cursoNuevo.Codigo)
                    .Where(g => g.Activo && g.Estado != "Cancelado")
                    .Select(g => new GrupoComboItem
                    {
                        Id = g.Id,
                        Codigo = g.Codigo,
                        Nombre = string.IsNullOrWhiteSpace(g.Nombre) ? "Sin Horario" : g.Nombre,
                        FechaInicio = g.FechaInicio
                    }).ToList();

                cmbCursoModalNuevoGrupo.ItemsSource = grupos;
                cmbCursoModalNuevoGrupo.IsEnabled = grupos.Count > 0;
                if (grupos.Count > 0) cmbCursoModalNuevoGrupo.SelectedIndex = 0;
            }
            else
            {
                _cursoDestinoSeleccionado = null;
                cmbCursoModalNuevoGrupo.ItemsSource = null;
                cmbCursoModalNuevoGrupo.IsEnabled = false;
            }
            ActualizarTextosComparativos();
        }

        private void CmbCursoModalNuevoGrupo_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void ChkCredito_Changed(object sender, RoutedEventArgs e) => ActualizarTextosComparativos();
        private void ChkInscripcion_Changed(object sender, RoutedEventArgs e) => ActualizarTextosComparativos();

        private void ActualizarTextosComparativos()
        {
            if (txtInfoCreditoClases == null || txtInfoDiferenciaInscripcion == null) return;

            // 1. Mensaje de Crédito
            if (chkTransferirTodoCredito.IsChecked == true)
            {
                txtInfoCreditoClases.Text = $"* Se transferirán {_totalPagadoClasesOrigen:N2} $ completos como crédito para el nuevo curso.";
            }
            else
            {
                txtInfoCreditoClases.Text = $"* Clases vistas se consideran consumidas. Solo se transferirá el saldo adelantado: {_saldoAdelantadoOrigen:N2} $.";
            }

            // 2. Mensaje de Matrícula
            if (chkConvalidarInscripcion.IsChecked == true)
            {
                decimal matOrigen = _cursoOrigenActual?.PrecioInscripcion ?? 0m;
                decimal matDestino = _cursoDestinoSeleccionado?.PrecioInscripcion ?? 0m;
                decimal diferencia = Math.Max(0, matDestino - matOrigen);

                if (diferencia > 0)
                    txtInfoDiferenciaInscripcion.Text = $"Matrícula convalidada. Debe pagar diferencia de: {diferencia:N2} $";
                else
                    txtInfoDiferenciaInscripcion.Text = "Matrícula convalidada al 100% (Diferencia: 0.00 $)";
            }
            else
            {
                decimal matCompleta = _cursoDestinoSeleccionado?.PrecioInscripcion ?? 0m;
                txtInfoDiferenciaInscripcion.Text = $"Debe pagar inscripción completa del nuevo curso: {matCompleta:N2} $";
            }
        }

        private void BtnCerrarModalCambioCurso_Click(object sender, RoutedEventArgs e)
        {
            modalCambioCurso.Visibility = Visibility.Collapsed;
        }

        private void BtnConfirmarCambioCurso_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionada == null) return;

            if (cmbCursoModalNuevoCurso.SelectedItem is not Curso cursoNuevo ||
                cmbCursoModalNuevoGrupo.SelectedItem is not GrupoComboItem grupoNuevo)
            {
                AlertaWindow.MostrarAdvertencia("Debe seleccionar la nueva carrera y el horario correspondiente.");
                return;
            }

            string motivo = txtCursoModalMotivo.Text.Trim();
            if (string.IsNullOrWhiteSpace(motivo) || motivo.Length < 4)
            {
                AlertaWindow.MostrarAdvertencia("Debe indicar el motivo del cambio de carrera (mínimo 4 caracteres).");
                return;
            }

            // Validación de Administrador (Regla 5)
            string adminAutoriza = SesionService.Instancia.UsuarioActual?.Username ?? "Admin";
            if (!SesionService.Instancia.EsAdministrador())
            {
                string userAdmin = txtAdminUser.Text.Trim();
                string passAdmin = txtAdminPass.Password;

                if (!_usuarioService.ValidarCredenciales(userAdmin, passAdmin, out var usuarioAdmin) ||
                    (usuarioAdmin!.Rol != "Admin" && usuarioAdmin.Rol != "Programador"))
                {
                    AlertaWindow.MostrarError("Autorización rechazada: Usuario o contraseña de Administrador incorrectos.");
                    return;
                }
                adminAutoriza = usuarioAdmin.Username;
            }

            // Confirmación explícita de deuda (Regla 3)
            bool perdonar = chkPerdonarDeuda.IsChecked == true;
            if (_deudaActualOrigen > 0)
            {
                string mensajeDeuda = perdonar
                    ? $"El alumno debe {_deudaActualOrigen:N2} $. Ha seleccionado PERDONAR la deuda.\n¿Confirma condonar este monto y registrarlo en auditoría?"
                    : $"El alumno debe {_deudaActualOrigen:N2} $. La deuda NO será perdonada y quedará registrada en su expediente.\n¿Desea continuar?";

                if (!AlertaWindow.SolicitarConfirmacion(mensajeDeuda, "Gestión de Deuda"))
                    return;
            }

            try
            {
                bool transferirTodo = chkTransferirTodoCredito.IsChecked == true;
                bool convalidarMatricula = chkConvalidarInscripcion.IsChecked == true;

                _inscripcionService.ProcesarCambioDeCurso(
                    _seleccionada,
                    grupoNuevo.Id,
                    grupoNuevo.Codigo,
                    cursoNuevo.Nombre,
                    transferirTodo,
                    convalidarMatricula,
                    perdonar,
                    _deudaActualOrigen,
                    motivo,
                    adminAutoriza);

                AlertaWindow.MostrarExito($"Cambio de carrera procesado exitosamente hacia '{cursoNuevo.Nombre}'.");
                modalCambioCurso.Visibility = Visibility.Collapsed;
                CargarInscripciones();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al cambiar de curso: {ex.Message}");
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
                AlertaWindow.MostrarAdvertencia("Cédula, Nombre, Carrera y Horario son obligatorios.");
                return;
            }

            int grupoId = (int)cmbNuevoGrupo.SelectedValue;

            try
            {
                _estudianteService.CrearEstudiante(cedula, nombre, telefono, email, null);
                _inscripcionService.RegistrarInscripcion(cedula, grupoId, "Inscripción en nuevo sistema");

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