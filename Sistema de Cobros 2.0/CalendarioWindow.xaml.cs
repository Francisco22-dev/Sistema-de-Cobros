using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    // Modelo de presentación para el ComboBox de grupos
    public class HorarioItemUI
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }

        // Muestra claramente el estado (incluyendo Finalizados) y detalles
        public string Display => $"[{Estado}] {Nombre} ({Codigo}) - Inicia: {FechaInicio:dd/MM/yyyy}";
    }

    public partial class CalendarioWindow : Window
    {
        private readonly CalendarioService _calendarioService = new CalendarioService();
        private readonly GrupoService _grupoService = new GrupoService();
        private readonly CursoService _cursoService = new CursoService();

        // Almacena la lista de horarios del curso actual antes de filtrar
        private List<HorarioItemUI> _horariosCursoActual = new List<HorarioItemUI>();

        public CalendarioWindow()
        {
            InitializeComponent();
            CargarCursos();
        }

        private void CargarCursos()
        {
            try
            {
                // Carga los cursos activos del instituto
                var cursos = _cursoService.ObtenerCursos().Where(c => c.Activo).ToList();
                cmbCursos.ItemsSource = cursos;
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al cargar cursos: {ex.Message}");
            }
        }

        private void CmbCursos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // REGLA 2: Limpiar inmediatamente la grilla y el buscador al cambiar de curso
            dgCalendario.ItemsSource = null;
            txtBuscarHorario.Text = string.Empty;
            cmbGrupos.ItemsSource = null;
            btnGenerar.IsEnabled = false;

            if (cmbCursos.SelectedValue is string cursoCodigo)
            {
                // REGLA 4: Se obtienen todos los grupos (incluso los finalizados)
                var gruposDb = _grupoService.ObtenerPorCurso(cursoCodigo);

                _horariosCursoActual = gruposDb.Select(g => new HorarioItemUI
                {
                    Id = g.Id,
                    Codigo = g.Codigo,
                    Nombre = string.IsNullOrWhiteSpace(g.Nombre) ? "Sin Horario Asignado" : g.Nombre,
                    Estado = g.Estado,
                    FechaInicio = g.FechaInicio
                }).ToList();

                // REGLA 5: Verificar si el curso tiene horarios creados
                if (_horariosCursoActual.Count == 0)
                {
                    txtAvisoSinHorarios.Visibility = Visibility.Visible;
                    txtBuscarHorario.IsEnabled = false;
                    cmbGrupos.IsEnabled = false;
                }
                else
                {
                    txtAvisoSinHorarios.Visibility = Visibility.Collapsed;
                    txtBuscarHorario.IsEnabled = true;
                    cmbGrupos.IsEnabled = true;
                    cmbGrupos.ItemsSource = _horariosCursoActual;
                }
            }
            else
            {
                txtAvisoSinHorarios.Visibility = Visibility.Collapsed;
                txtBuscarHorario.IsEnabled = false;
                cmbGrupos.IsEnabled = false;
            }
        }

        private void TxtBuscarHorario_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = txtBuscarHorario.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(filtro))
            {
                cmbGrupos.ItemsSource = _horariosCursoActual;
            }
            else
            {
                // Filtro en tiempo real por nombre, código o estado
                var listaFiltrada = _horariosCursoActual.Where(h =>
                    h.Nombre.ToLower().Contains(filtro) ||
                    h.Codigo.ToLower().Contains(filtro) ||
                    h.Estado.ToLower().Contains(filtro)
                ).ToList();

                cmbGrupos.ItemsSource = listaFiltrada;
            }

            // Si hay opciones, se abre automáticamente el desplegable para facilitar la elección
            if (cmbGrupos.Items.Count > 0 && !string.IsNullOrWhiteSpace(filtro))
            {
                cmbGrupos.IsDropDownOpen = true;
            }
        }

        private void CmbGrupos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbGrupos.SelectedItem is HorarioItemUI grupoSeleccionado)
            {
                btnGenerar.IsEnabled = true;
                CargarClases(grupoSeleccionado.Id);
            }
            else
            {
                btnGenerar.IsEnabled = false;
                dgCalendario.ItemsSource = null;
            }
        }

        private void CargarClases(int grupoId)
        {
            try
            {
                dgCalendario.ItemsSource = _calendarioService.ObtenerPorGrupo(grupoId);
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al cargar clases: {ex.Message}");
            }
        }

        private void BtnGenerar_Click(object sender, RoutedEventArgs e)
        {
            if (!(cmbGrupos.SelectedItem is HorarioItemUI grupoSeleccionado))
            {
                AlertaWindow.MostrarAdvertencia("Por favor seleccione un horario o grupo primero.");
                return;
            }

            try
            {
                // REGLA 3: Verificar si el grupo ya tiene clases registradas y solicitar confirmación
                var clasesActuales = _calendarioService.ObtenerPorGrupo(grupoSeleccionado.Id);
                if (clasesActuales.Count > 0)
                {
                    bool confirmado = AlertaWindow.SolicitarConfirmacion(
                        $"Este grupo ya cuenta con {clasesActuales.Count} clase(s) registradas en el calendario.\n\n¿Desea generar/completar las clases faltantes de este curso?",
                        "Confirmar Generación");

                    if (!confirmado) return;
                }

                _calendarioService.GenerarClasesAutomaticas(grupoSeleccionado.Id);
                AlertaWindow.MostrarExito("Clases del calendario generadas exitosamente.");
                CargarClases(grupoSeleccionado.Id);
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al generar clases: {ex.Message}");
            }
        }

        private void CambiarEstado(object sender, string estado)
        {
            if (sender is Button btn && btn.DataContext is ClaseProgramada clase)
            {
                try
                {
                    _calendarioService.CambiarEstado(clase.Id, estado);
                    if (cmbGrupos.SelectedItem is HorarioItemUI grupo)
                    {
                        CargarClases(grupo.Id);
                    }
                }
                catch (Exception ex)
                {
                    AlertaWindow.MostrarError($"Error al actualizar la clase: {ex.Message}");
                }
            }
        }

        private void BtnRealizada_Click(object sender, RoutedEventArgs e) => CambiarEstado(sender, "Realizada");
        private void BtnNula_Click(object sender, RoutedEventArgs e) => CambiarEstado(sender, "Nula");
    }
}