using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public partial class CursosWindow : Window
    {
        private readonly CursoService _cursoService = new CursoService();
        private readonly GrupoService _grupoService = new GrupoService();

        private Curso? _cursoSeleccionado;
        private Grupo? _grupoSeleccionado;

        public CursosWindow()
        {
            InitializeComponent();
            dpFechaInicio.SelectedDate = DateTime.Now;
            CargarCursos();
        }

        private void CargarCursos()
        {
            dgCursos.ItemsSource = _cursoService.ObtenerCursos();
        }

        private void TxtBuscarCurso_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = txtBuscarCurso.Text.ToLower();
            dgCursos.ItemsSource = _cursoService.ObtenerCursos()
                .Where(c => c.Nombre.ToLower().Contains(filtro) || c.Codigo.ToLower().Contains(filtro)).ToList();
        }

        private void DgCursos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCursos.SelectedItem is Curso curso)
            {
                _cursoSeleccionado = curso;
                txtAccionCurso.Text = "Editando Curso: " + curso.Codigo;
                txtCodigoCurso.Text = curso.Codigo;
                txtCodigoCurso.IsEnabled = false;
                txtNombreCurso.Text = curso.Nombre;
                txtMeses.Text = (curso.DuracionSemanas / 4).ToString();
                txtPrecioInsc.Text = curso.PrecioInscripcion.ToString();
                txtPrecioClase.Text = curso.PrecioMensualidad.ToString();
                txtPrecioInscGrupo.Text = curso.PrecioInscripcion.ToString("N2");
                txtPrecioClaseGrupo.Text = curso.PrecioMensualidad.ToString("N2");

                btnActivarCurso.Visibility = Visibility.Visible;
                btnActivarCurso.Content = curso.Activo ? "Desactivar" : "Activar";
                btnActivarCurso.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(curso.Activo ? "#F44336" : "#4CAF50"));

                txtTituloHorarios.Text = $"Horarios para: {curso.Nombre}";
                panelHorarios.IsEnabled = curso.Activo;
                CargarHorariosDelCurso();
                ResetearFormularioHorario();
            }
        }

        private void CargarHorariosDelCurso()
        {
            if (_cursoSeleccionado != null)
            {
                dgHorarios.ItemsSource = _grupoService.ObtenerPorCurso(_cursoSeleccionado.Codigo);
            }
        }

        private void ResetearFormularioHorario()
        {
            _grupoSeleccionado = null;
            dgHorarios.SelectedItem = null;
            if (_cursoSeleccionado != null)
            {
                txtTituloPanelHorario.Text = "Agregar Horario / Grupo al Curso";
                txtTituloPanelHorario.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                txtCodigoGrupo.IsEnabled = true;
                txtCodigoGrupo.Text = _grupoService.SugerirCodigoGrupo(_cursoSeleccionado.Codigo);
                dpFechaInicio.SelectedDate = DateTime.Now;
                btnGuardarHorario.Content = "Agregar Horario (Grupo)";
                btnGuardarHorario.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                btnCancelarEdicionHorario.Visibility = Visibility.Collapsed;
            }
        }

        // ========================================================
        // EDICIÓN DE HORARIO SELECCIONADO
        // ========================================================

        private void DgHorarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgHorarios.SelectedItem is Grupo grupo)
            {
                _grupoSeleccionado = grupo;
                txtTituloPanelHorario.Text = $"Editando Grupo: {grupo.Codigo}";
                txtTituloPanelHorario.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2")); // Azul
                txtCodigoGrupo.Text = grupo.Codigo;
                txtCodigoGrupo.IsEnabled = false; // El código único no se altera
                dpFechaInicio.SelectedDate = grupo.FechaInicio;
                txtPrecioInscGrupo.Text = (grupo.PrecioInscripcion ?? _cursoSeleccionado?.PrecioInscripcion ?? 0m).ToString("N2");
                txtPrecioClaseGrupo.Text = (grupo.PrecioMensualidad ?? _cursoSeleccionado?.PrecioMensualidad ?? 0m).ToString("N2");

                // Intentar coincidir los desplegables con el texto del nombre
                string nombre = grupo.Nombre ?? "";
                foreach (ComboBoxItem item in cmbDias.Items)
                {
                    if (nombre.Contains(item.Content.ToString()!))
                    {
                        cmbDias.SelectedItem = item;
                        break;
                    }
                }
                foreach (ComboBoxItem item in cmbHoras.Items)
                {
                    if (nombre.Contains(item.Content.ToString()!))
                    {
                        cmbHoras.SelectedItem = item;
                        break;
                    }
                }

                btnGuardarHorario.Content = "Actualizar Horario / Fecha";
                btnGuardarHorario.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1976D2"));
                btnCancelarEdicionHorario.Visibility = Visibility.Visible;
            }
        }

        private void BtnCancelarEdicionHorario_Click(object sender, RoutedEventArgs e)
        {
            ResetearFormularioHorario();
        }

        private void BtnGuardarHorario_Click(object sender, RoutedEventArgs e)
        {
            if (_cursoSeleccionado == null || dpFechaInicio.SelectedDate == null) return;

            string codigoGrupo = txtCodigoGrupo.Text.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(codigoGrupo))
            {
                AlertaWindow.MostrarAdvertencia("Debe ingresar un código para el grupo.");
                return;
            }

            string horarioConcat = $"{((ComboBoxItem)cmbDias.SelectedItem).Content} {((ComboBoxItem)cmbHoras.SelectedItem).Content}";
            DateTime fechaNueva = dpFechaInicio.SelectedDate.Value;

            try
            {
                var cultura = new CultureInfo("es-VE");
                decimal.TryParse(txtPrecioInscGrupo.Text.Trim(), NumberStyles.Any, cultura, out decimal pInscGrupo);
                decimal.TryParse(txtPrecioClaseGrupo.Text.Trim(), NumberStyles.Any, cultura, out decimal pClaseGrupo);
                if (_grupoSeleccionado == null)
                {
                    // CREAR NUEVO
                    _grupoService.GuardarHorario(_cursoSeleccionado.Codigo, codigoGrupo, horarioConcat, fechaNueva, pInscGrupo, pClaseGrupo);
                    AlertaWindow.MostrarExito($"Grupo '{codigoGrupo}' creado exitosamente.");
                }
                else
                {
                    // EDITAR EXISTENTE
                    bool cambioFecha = _grupoSeleccionado.FechaInicio.Date != fechaNueva.Date;
                    _grupoSeleccionado.Nombre = horarioConcat;
                    _grupoSeleccionado.FechaInicio = fechaNueva;
                    _grupoSeleccionado.PrecioInscripcion = pInscGrupo;
                    _grupoSeleccionado.PrecioMensualidad = pClaseGrupo;

                    _grupoService.ActualizarHorario(_grupoSeleccionado, cambioFecha);

                    string detalleMsg = cambioFecha
                        ? $"Grupo '{_grupoSeleccionado.Codigo}' actualizado. Las 24 fechas del calendario fueron reajustadas automáticamente a la nueva fecha de inicio."
                        : $"Horario del grupo '{_grupoSeleccionado.Codigo}' actualizado exitosamente.";

                    AlertaWindow.MostrarExito(detalleMsg);
                }

                CargarHorariosDelCurso();
                ResetearFormularioHorario();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError(ex.Message);
            }
        }

        private void BtnLimpiarCurso_Click(object sender, RoutedEventArgs e)
        {
            _cursoSeleccionado = null;
            txtAccionCurso.Text = "Crear Nuevo Curso";
            txtCodigoCurso.Text = "";
            txtCodigoCurso.IsEnabled = true;
            txtNombreCurso.Text = "";
            txtMeses.Text = "";
            txtPrecioInsc.Text = "";
            txtPrecioClase.Text = "";
            txtCodigoGrupo.Text = "";
            btnActivarCurso.Visibility = Visibility.Collapsed;

            txtTituloHorarios.Text = "Seleccione un Curso...";
            panelHorarios.IsEnabled = false;
            dgHorarios.ItemsSource = null;
            dgCursos.SelectedItem = null;
        }

        private void BtnGuardarCurso_Click(object sender, RoutedEventArgs e)
        {
            var cultura = new CultureInfo("es-VE");
            if (string.IsNullOrWhiteSpace(txtCodigoCurso.Text) || string.IsNullOrWhiteSpace(txtNombreCurso.Text)) return;
            if (!int.TryParse(txtMeses.Text, out int meses) || meses <= 0) return;
            if (!decimal.TryParse(txtPrecioInsc.Text, NumberStyles.Any, cultura, out decimal pInsc) ||
                !decimal.TryParse(txtPrecioClase.Text, NumberStyles.Any, cultura, out decimal pClase)) return;

            try
            {
                if (_cursoSeleccionado == null)
                {
                    _cursoService.GuardarCurso(txtCodigoCurso.Text.Trim(), txtNombreCurso.Text.Trim(), meses, pInsc, pClase);
                }
                else
                {
                    _cursoSeleccionado.Nombre = txtNombreCurso.Text.Trim();
                    _cursoSeleccionado.DuracionSemanas = meses * 4;
                    _cursoSeleccionado.PrecioInscripcion = pInsc;
                    _cursoSeleccionado.PrecioMensualidad = pClase;
                    _cursoService.ActualizarCurso(_cursoSeleccionado);
                }
                AlertaWindow.MostrarExito("Curso guardado.");
                CargarCursos();
                BtnLimpiarCurso_Click(null, null);
            }
            catch (Exception ex) { AlertaWindow.MostrarError(ex.Message); }
        }

        private void BtnActivarCurso_Click(object sender, RoutedEventArgs e)
        {
            if (_cursoSeleccionado != null)
            {
                _cursoService.CambiarEstadoCurso(_cursoSeleccionado.Codigo, !_cursoSeleccionado.Activo);
                CargarCursos();
                BtnLimpiarCurso_Click(null, null);
            }
        }
    }
}