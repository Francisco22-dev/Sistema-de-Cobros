using System.Windows;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    /// <summary>
    /// Lógica de interacción para AuditoriaWindow.xaml
    /// </summary>
    public partial class AuditoriaWindow : Window
    {
        private readonly AuditorService _auditorService;

        public AuditoriaWindow()
        {
            InitializeComponent();
            _auditorService = new AuditorService();
            CargarAuditoria();
        }

        private void CargarAuditoria()
        {
            dgAuditoria.ItemsSource = _auditorService.ObtenerRegistros();
        }
    }
}
