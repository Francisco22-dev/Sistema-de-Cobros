using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public partial class EstudiantesWindow : Window
    {
        private readonly EstudianteService _estudianteService = new EstudianteService();
        private readonly InscripcionService _inscripcionService = new InscripcionService();
        private readonly GrupoService _grupoService = new GrupoService();
        private readonly CursoService _cursoService = new CursoService();
        private readonly UsuarioService _usuarioService = new UsuarioService();

        private InscripcionInfo? _seleccionada;
        private List<Curso> _cursosActivos = new List<Curso>();

        // Variables de balance para cambio de curso
        private int _clasesPagadasOrigen = 0;
        private decimal _deudaActualOrigen = 0m;
        private decimal _precioClaseOrigen = 14m;
        private Curso? _cursoOrigenActual;
        private Curso? _cursoDestinoSeleccionado;

        public EstudiantesWindow()
        {
            InitializeComponent();
            CargarCursosEnCascada();
            CargarInscripciones();
        }

        // ========================================================
        // VALIDACIÓN DE DATOS EN TIEMPO REAL (TECLADO)
        // ========================================================

        private void ValidarSoloNumeros_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Solo permite ingresar dígitos del 0 al 9
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private bool ValidarFormularioRegistro(out string cedula, out string nombre, out string telefono, out string email, out int grupoId)
        {
            cedula = txtCedula.Text.Trim();
            nombre = txtNombre.Text.Trim();
            telefono = txtTelefono.Text.Trim();
            email = txtEmail.Text.Trim();
            grupoId = 0;

            // 1. Validación de Cédula
            if (string.IsNullOrWhiteSpace(cedula) || !Regex.IsMatch(cedula, "^[0-9]{6,10}$"))
            {
                AlertaWindow.MostrarAdvertencia("La cédula debe contener solo números y tener entre 6 y 10 dígitos.");
                txtCedula.Focus();
                return false;
            }

            // 2. Validación de Nombre
            if (string.IsNullOrWhiteSpace(nombre) || nombre.Length < 3 || !Regex.IsMatch(nombre, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$"))
            {
                AlertaWindow.MostrarAdvertencia("Ingrese un nombre completo válido (mínimo 3 letras, sin números ni símbolos).");
                txtNombre.Focus();
                return false;
            }

            // 3. Validación de Teléfono
            if (string.IsNullOrWhiteSpace(telefono) || !Regex.IsMatch(telefono, "^[0-9]{10,11}$"))
            {
                AlertaWindow.MostrarAdvertencia("El teléfono debe contener solo números y tener 11 dígitos (Ej: 04121234567).");
                txtTelefono.Focus();
                return false;
            }

            // 4. Validación de Correo (Opcional)
            if (!string.IsNullOrWhiteSpace(email))
            {
                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    AlertaWindow.MostrarAdvertencia("El correo electrónico ingresado no tiene un formato válido (Ej: alumno@gmail.com).");
                    txtEmail.Focus();
                    return false;
                }
            }
            else
            {
                email = "noposee@gmail.com";
            }

            // 5. Validación de Selección de Horario
            if (cmbNuevoCurso.SelectedValue == null || cmbNuevoGrupo.SelectedValue == null)
            {
                AlertaWindow.MostrarAdvertencia("Debe seleccionar una Carrera y un Horario/Cohorte.");
                return false;
            }

            grupoId = (int)cmbNuevoGrupo.SelectedValue;
            return true;
        }

        // ========================================================
        // REGISTRO DE ALUMNO CON REGLA MULTICURSO Y NO DUPLICIDAD
        // ========================================================

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormularioRegistro(out string cedula, out string nombre, out string telefono, out string email, out int grupoId))
                return;

            string cursoCodigo = (cmbNuevoCurso.SelectedValue as string) ?? "";

            // REGLAS 6 Y 7: VALIDAR INSCRIPCIÓN EN ESE CURSO
            var historial = _inscripcionService.ValidarHistorialInscripcion(cedula, cursoCodigo);

            if (historial.Existe)
            {
                if (historial.EstaActiva)
                {
                    // Bloqueo estricto: ya está cursando esa misma carrera actualmente
                    AlertaWindow.MostrarError(
                        $"El estudiante {nombre} (C.I. {cedula}) ya tiene una inscripción ACTIVA en el curso '{historial.CursoNombre}' ([{historial.GrupoCodigo}]).\n\nNo se permite duplicar la inscripción en el mismo curso.");
                    return;
                }
                else
                {
                    // Alerta visual informativa si lo cursó antes y lo canceló/finalizó
                    bool continuar = AlertaWindow.SolicitarConfirmacion(
                        $"AVISO PREVIO:\nEl estudiante ya cursó anteriormente '{historial.CursoNombre}' (Estado previo: {historial.EstadoPrevio} en el grupo [{historial.GrupoCodigo}]).\n\n¿Desea registrar una nueva inscripción para que vuelva a cursar la carrera?",
                        "Inscripción Previa Detectada");

                    if (!continuar) return;
                }
            }

            try
            {
                _estudianteService.CrearEstudiante(cedula, nombre, telefono, email, null);
                _inscripcionService.RegistrarInscripcion(cedula, grupoId, "Inscripción en sistema");

                AlertaWindow.MostrarExito($"Estudiante '{nombre}' inscrito exitosamente en {cmbNuevoCurso.Text}.");

                txtCedula.Clear();
                txtNombre.Clear();
                txtTelefono.Clear();
                txtEmail.Clear();
                cmbNuevoCurso.SelectedIndex = -1;
                CargarInscripciones();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al inscribir: {ex.Message}");
            }
        }

        // ========================================================
        // CARGA EN CASCADA
        // ========================================================

        private void CargarCursosEnCascada()
        {
            try
            {
                _cursosActivos = _cursoService.ObtenerCursos().Where(c => c.Activo).ToList();
                cmbNuevoCurso.ItemsSource = _cursosActivos;
            }
            catch (Exception ex) { AlertaWindow.MostrarError(ex.Message); }
        }

        private void CmbNuevoCurso_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbNuevoCurso.SelectedItem is Curso cursoSeleccionado)
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
            catch (Exception ex) { AlertaWindow.MostrarError(ex.Message); }
        }

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e) => CargarInscripciones();
        private void CmbFiltroEstado_SelectionChanged(object sender, SelectionChangedEventArgs e) => CargarInscripciones();

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

        private void BtnCerrarModalCambio_Click(object sender, RoutedEventArgs e) => modalCambioGrupo.Visibility = Visibility.Collapsed;

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
                    _seleccionada.InscripcionId, nuevoGrupo.Id, _seleccionada.GrupoCodigo, nuevoGrupo.Codigo, _seleccionada.Observaciones ?? "", txtModalMotivo.Text);

                AlertaWindow.MostrarExito($"Estudiante trasladado al grupo [{nuevoGrupo.Codigo}]. Pagos conservados.");
                modalCambioGrupo.Visibility = Visibility.Collapsed;
                CargarInscripciones();
            }
            catch (Exception ex) { AlertaWindow.MostrarError(ex.Message); }
        }

        // ========================================================
        // MODAL 2: CAMBIO DE CURSO / CARRERA (POR CLASES 1 A 1)
        // ========================================================

        private void BtnAbrirModalCambioCurso_Click(object sender, RoutedEventArgs e)
        {
            if (_seleccionada == null) return;

            _cursoOrigenActual = _cursosActivos.FirstOrDefault(c => c.Codigo == _seleccionada.CursoCodigo);
            _precioClaseOrigen = _cursoOrigenActual?.PrecioMensualidad ?? 14m;

            // Obtener balance actual del estudiante
            var balance = _inscripcionService.ObtenerBalanceEstudianteEnCurso(_seleccionada.InscripcionId, _seleccionada.GrupoId, _precioClaseOrigen);
            _deudaActualOrigen = balance.DeudaUSD;

            // Cantidad de clases que pagó en el curso actual
            _clasesPagadasOrigen = _precioClaseOrigen > 0 ? (int)(balance.TotalPagadoClasesUSD / _precioClaseOrigen) : 0;

            txtCursoModalAlumno.Text = $"Estudiante: {_seleccionada.NombreCompleto} (C.I. {_seleccionada.Cedula})";
            txtCursoModalOrigen.Text = $"Curso que abandona: {_seleccionada.CursoNombre} (Grupo: [{_seleccionada.GrupoCodigo}])";
            txtCursoModalClases.Text = $"Clases pagadas en este curso: {_clasesPagadasOrigen} clases ({balance.TotalPagadoClasesUSD:N2} $)";

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

            // Cursos disponibles distintos al actual
            var otrosCursos = _cursosActivos.Where(c => c.Codigo != _seleccionada.CursoCodigo).ToList();
            cmbCursoModalNuevoCurso.ItemsSource = otrosCursos;
            cmbCursoModalNuevoGrupo.ItemsSource = null;
            cmbCursoModalNuevoGrupo.IsEnabled = false;

            ActualizarCondicionesClases();
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
            ActualizarCondicionesClases();
        }

        private void CmbCursoModalNuevoGrupo_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void ChkCobrarDiferencia_Changed(object sender, RoutedEventArgs e) => ActualizarCondicionesClases();

        private void ActualizarCondicionesClases()
        {
            if (txtInfoClasesReconocidas == null) return;

            txtInfoClasesReconocidas.Text = $"• Se acreditarán {_clasesPagadasOrigen} clase(s) en la nueva carrera.";

            if (_cursoDestinoSeleccionado != null && _cursoDestinoSeleccionado.PrecioMensualidad > _precioClaseOrigen)
            {
                // Curso nuevo es más caro
                pnlDiferenciaPrecio.Visibility = Visibility.Visible;
                decimal difPorClase = _cursoDestinoSeleccionado.PrecioMensualidad - _precioClaseOrigen;
                decimal totalDif = difPorClase * _clasesPagadasOrigen;
                txtInfoDiferencia.Text = $"Diferencia por clase ({difPorClase:N2} $ c/u) • Total a pagar: {totalDif:N2} $";
            }
            else
            {
                pnlDiferenciaPrecio.Visibility = Visibility.Collapsed;
                chkCobrarDiferencia.IsChecked = false;
            }
        }

        private void BtnCerrarModalCambioCurso_Click(object sender, RoutedEventArgs e) => modalCambioCurso.Visibility = Visibility.Collapsed;

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

            // Permiso Admin si es Cajero
            string adminAutoriza = SesionService.Instancia.UsuarioActual?.Username ?? "Admin";
            if (!SesionService.Instancia.EsAdministrador())
            {
                string userAdmin = txtAdminUser.Text.Trim();
                string passAdmin = txtAdminPass.Password;

                if (!_usuarioService.ValidarCredenciales(userAdmin, passAdmin, out var usuarioAdmin) ||
                    (usuarioAdmin!.Rol != "Admin" && usuarioAdmin.Rol != "Programador"))
                {
                    AlertaWindow.MostrarError("Autorización rechazada: Credenciales de Administrador incorrectas.");
                    return;
                }
                adminAutoriza = usuarioAdmin.Username;
            }

            // Confirmación explícita de deuda
            bool perdonar = chkPerdonarDeuda.IsChecked == true;
            if (_deudaActualOrigen > 0)
            {
                string msg = perdonar
                    ? $"El alumno adeuda {_deudaActualOrigen:N2} $. Ha elegido PERDONAR la deuda.\n¿Confirma condonar este monto?"
                    : $"El alumno adeuda {_deudaActualOrigen:N2} $. La deuda NO será perdonada y quedará asentada.\n¿Desea continuar?";

                if (!AlertaWindow.SolicitarConfirmacion(msg, "Gestión de Deuda"))
                    return;
            }

            try
            {
                bool cobrarDiferencia = chkCobrarDiferencia.IsChecked == true;

                _inscripcionService.ProcesarCambioCursoPorClases(
                    _seleccionada,
                    grupoNuevo.Id,
                    grupoNuevo.Codigo,
                    cursoNuevo,
                    _clasesPagadasOrigen,
                    cobrarDiferencia,
                    _precioClaseOrigen,
                    perdonar,
                    _deudaActualOrigen,
                    motivo,
                    adminAutoriza);

                AlertaWindow.MostrarExito($"Cambio de carrera procesado hacia '{cursoNuevo.Nombre}'. Se reconocieron {_clasesPagadasOrigen} clases.");
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

            if (!AlertaWindow.SolicitarConfirmacion($"¿Está seguro de marcar la inscripción de '{_seleccionada.NombreCompleto}' como {nuevoEstado.ToUpper()}?", $"Confirmar {accionNombre}"))
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
    }
}