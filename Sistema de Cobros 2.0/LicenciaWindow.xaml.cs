using System;
using System.Windows;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class LicenciaWindow : Window
    {
        private readonly LicenciaService _licenciaService = new LicenciaService();

        public LicenciaWindow()
        {
            InitializeComponent();
            dpFecha.SelectedDate = DateTime.Now.AddYears(1);
        }

        private void BtnRenovar_Click(object sender, RoutedEventArgs e)
        {
            if (dpFecha.SelectedDate == null) return;
            try
            {
                _licenciaService.RenovarLicencia(txtClave.Password, dpFecha.SelectedDate.Value);
                AlertaWindow.MostrarExito("Licencia renovada con éxito.");
                this.Close();
            }
            catch (Exception ex) { AlertaWindow.MostrarError(ex.Message); }
        }
    }
}