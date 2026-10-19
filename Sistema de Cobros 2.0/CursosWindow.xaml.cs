using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public partial class CursosWindow : Window
    {
        private readonly CursoService _cursoService = new CursoService();
        private readonly GrupoService _grupoService = new GrupoService();
        private Curso? _cursoSeleccionado;

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

                btnActivarCurso.Visibility = Visibility.Visible;
                btnActivarCurso.Content = curso.Activo ? "Desactivar" : "Activar";
                btnActivarCurso.Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(curso.Activo ? "#F44336" : "#4CAF50"));

                // Cargar panel derecho
                txtTituloHorarios.Text = $"Horarios para: {curso.Nombre}";
                panelHorarios.IsEnabled = curso.Activo;
                dgHorarios.ItemsSource = _grupoService.ObtenerPorCurso(curso.Codigo);
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

        private void BtnAgregarHorario_Click(object sender, RoutedEventArgs e)
        {
            if (_cursoSeleccionado == null || dpFechaInicio.SelectedDate == null) return;

            string horarioConcat = $"{((ComboBoxItem)cmbDias.SelectedItem).Content} {txtHoraInicio.Text} - {txtHoraFin.Text}";

            try
            {
                _grupoService.GuardarHorario(_cursoSeleccionado.Codigo, horarioConcat, dpFechaInicio.SelectedDate.Value);
                AlertaWindow.MostrarExito("Horario/Grupo generado automáticamente.");
                dgHorarios.ItemsSource = _grupoService.ObtenerPorCurso(_cursoSeleccionado.Codigo);
            }
            catch (Exception ex) { AlertaWindow.MostrarError(ex.Message); }
        }
    }
}