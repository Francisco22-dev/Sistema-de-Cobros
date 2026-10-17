using System;
using System.Windows;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class CierreCajaWindow : Window
    {
        private readonly CajaService _cajaService = new CajaService();
        public bool CajaCerradaExitosamente { get; private set; } = false;

        public CierreCajaWindow()
        {
            InitializeComponent();
            CargarArqueo();
        }

        private void CargarArqueo()
        {
            try
            {
                var arqueo = _cajaService.GenerarArqueo();
                dgArqueo.ItemsSource = arqueo;

                if (arqueo.Count == 0)
                {
                    AlertaWindow.MostrarInfo("No se registraron cobros durante este turno de caja.");
                }
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError("Error al cargar arqueo: " + ex.Message);
            }
        }

        private void BtnConfirmarCierre_Click(object sender, RoutedEventArgs e)
        {
            if (AlertaWindow.SolicitarConfirmacion("Una vez cerrada la caja, no podrá procesar más cobros hoy a menos que abra un turno nuevo.\n\n¿Desea cerrar la caja?", "Cierre Definitivo"))
            {
                try
                {
                    _cajaService.CerrarCaja();
                    CajaCerradaExitosamente = true;
                    AlertaWindow.MostrarExito("La caja ha sido cerrada correctamente. Puede retirarse.");
                    this.Close();
                }
                catch (Exception ex)
                {
                    AlertaWindow.MostrarError(ex.Message);
                }
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}