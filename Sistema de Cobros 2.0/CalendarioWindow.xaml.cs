using System.Windows;
using System.Windows.Controls;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class CalendarioWindow : Window
    {
        private readonly CalendarioService _calendarioService = new CalendarioService();
        private readonly GrupoService _grupoService = new GrupoService();

        public CalendarioWindow()
        {
            InitializeComponent();
            cmbGrupos.ItemsSource = _grupoService.ObtenerGruposActivos();
        }

        private void CargarClases()
        {
            if (cmbGrupos.SelectedValue != null)
                dgCalendario.ItemsSource = _calendarioService.ObtenerPorGrupo((int)cmbGrupos.SelectedValue);
        }

        private void CmbGrupos_SelectionChanged(object sender, SelectionChangedEventArgs e) => CargarClases();

        private void BtnGenerar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbGrupos.SelectedValue == null) return;
            try
            {
                _calendarioService.GenerarClasesAutomaticas((int)cmbGrupos.SelectedValue);
                AlertaWindow.MostrarExito("Calendario generado correctamente.");
                CargarClases();
            }
            catch (System.Exception ex) { AlertaWindow.MostrarError(ex.Message); }
        }

        private void CambiarEstado(object sender, string estado)
        {
            if (sender is Button btn && btn.DataContext is Entidades.ClaseProgramada clase)
            {
                _calendarioService.CambiarEstado(clase.Id, estado);
                CargarClases();
            }
        }

        private void BtnRealizada_Click(object sender, RoutedEventArgs e) => CambiarEstado(sender, "Realizada");
        private void BtnNula_Click(object sender, RoutedEventArgs e) => CambiarEstado(sender, "Nula");
    }
}