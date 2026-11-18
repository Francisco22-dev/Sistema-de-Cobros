using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public class HorarioItemUI
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public string Display => $"[{Estado}] {Nombre} ({Codigo}) - Inicia: {FechaInicio:dd/MM/yyyy}";
    }

    public partial class CalendarioWindow : Window
    {
        private readonly CalendarioService _calendarioService = new CalendarioService();
        private readonly GrupoService _grupoService = new GrupoService();
        private readonly CursoService _cursoService = new CursoService();

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
                cmbCursos.ItemsSource = _cursoService.ObtenerCursos().Where(c => c.Activo).ToList();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error cargando cursos: {ex.Message}");
            }
        }

        private void CmbCursos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dgCalendario.ItemsSource = null;
            txtBuscarHorario.Text = string.Empty;
            cmbGrupos.ItemsSource = null;
            btnGenerar.IsEnabled = false;
            txtIndicadorSemana.Text = "📍 Progreso: Seleccione un horario para ver el avance automático...";
            txtDetalleProgreso.Text = "Las clases anteriores a hoy se consideran vistas automáticamente sin necesidad de marcarlas.";

            if (cmbCursos.SelectedValue is string cursoCodigo)
            {
                var gruposDb = _grupoService.ObtenerPorCurso(cursoCodigo);

                _horariosCursoActual = gruposDb.Select(g => new HorarioItemUI
                {
                    Id = g.Id,
                    Codigo = g.Codigo,
                    Nombre = string.IsNullOrWhiteSpace(g.Nombre) ? "Sin Horario" : g.Nombre,
                    Estado = g.Estado,
                    FechaInicio = g.FechaInicio
                }).ToList();

                cmbGrupos.IsEnabled = _horariosCursoActual.Count > 0;
                txtBuscarHorario.IsEnabled = _horariosCursoActual.Count > 0;
                cmbGrupos.ItemsSource = _horariosCursoActual;
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
                var listaFiltrada = _horariosCursoActual.Where(h =>
                    h.Nombre.ToLower().Contains(filtro) ||
                    h.Codigo.ToLower().Contains(filtro) ||
                    h.Estado.ToLower().Contains(filtro)
                ).ToList();

                cmbGrupos.ItemsSource = listaFiltrada;
            }
        }

        private void CmbGrupos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbGrupos.SelectedItem is HorarioItemUI grupoSeleccionado)
            {
                btnGenerar.IsEnabled = true;
                CargarClases(grupoSeleccionado.Id);
                ActualizarIndicadorProgreso(grupoSeleccionado.Id);
            }
            else
            {
                btnGenerar.IsEnabled = false;
                dgCalendario.ItemsSource = null;
                txtIndicadorSemana.Text = "📍 Progreso: Seleccione un horario...";
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
                AlertaWindow.MostrarError($"Error cargando clases: {ex.Message}");
            }
        }

        // ========================================================
        // CÁLCULO DE PROGRESO 100% AUTOMÁTICO (SOLO PARA VER)
        // ========================================================
        private void ActualizarIndicadorProgreso(int grupoId)
        {
            try
            {
                var (impartidas, total, nulas, proxima) = _calendarioService.ObtenerResumenAutomatico(grupoId);

                if (total > 0)
                {
                    int pendientes = Math.Max(0, total - impartidas);
                    string proximaTexto = proxima.HasValue
                        ? $"Próxima clase: {proxima.Value:dd/MM/yyyy}"
                        : "Curso completado";

                    string canceladasTexto = nulas > 0 ? $" • {nulas} cancelada(s)" : "";

                    txtIndicadorSemana.Text = $"📍 AVANCE DEL CURSO: Clase {impartidas} de {total}";
                    txtDetalleProgreso.Text = $"{impartidas} clase(s) impartidas a la fecha • {pendientes} pendiente(s){canceladasTexto} • {proximaTexto}";
                }
                else
                {
                    txtIndicadorSemana.Text = "📍 Progreso: Sin clases programadas.";
                    txtDetalleProgreso.Text = "Presione el botón 'Generar Clases' para crear el cronograma oficial de 24 semanas.";
                }
            }
            catch
            {
                txtIndicadorSemana.Text = "📍 Progreso: Calculando avance...";
            }
        }

        private void BtnGenerar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbGrupos.SelectedItem is not HorarioItemUI grupo) return;

            try
            {
                _calendarioService.GenerarClasesAutomaticas(grupo.Id);
                AlertaWindow.MostrarExito("Cronograma de 24 clases generado exitosamente.");
                CargarClases(grupo.Id);
                ActualizarIndicadorProgreso(grupo.Id);
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError(ex.Message);
            }
        }

        // ========================================================
        // EXCEPCIÓN: PROFESOR FALTÓ (NULA + SEMANA EXTRA)
        // ========================================================
        private void BtnNula_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ClaseProgramada clase && cmbGrupos.SelectedItem is HorarioItemUI grupo)
            {
                if (AlertaWindow.SolicitarConfirmacion("¿Desea marcar esta fecha como NULA (falta de profesor)?\n\nEl sistema agregará automáticamente una semana recuperativa al final del calendario.", "Confirmar Nula"))
                {
                    try
                    {
                        _calendarioService.AnularYReprogramarClase(clase.Id, grupo.Id, "Falta de profesor / Suspensión");
                        AlertaWindow.MostrarExito("Clase marcada como Nula y semana de reposición agregada al final del curso.");
                        CargarClases(grupo.Id);
                        ActualizarIndicadorProgreso(grupo.Id);
                    }
                    catch (Exception ex)
                    {
                        AlertaWindow.MostrarError(ex.Message);
                    }
                }
            }
        }
    }
}