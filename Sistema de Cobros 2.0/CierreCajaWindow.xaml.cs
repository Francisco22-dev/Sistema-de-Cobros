using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class CierreCajaWindow : Window
    {
        private readonly CajaService _cajaService = new CajaService();
        private readonly MantenimientoService _mantenimientoService = new MantenimientoService();
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

                // Agrupación Estilo Libreta
                decimal fisicoBs = arqueo.Where(a => a.Moneda == "BS" && a.Metodo.Contains("Efectivo")).Sum(a => a.Total);
                decimal bancoBs = arqueo.Where(a => a.Moneda == "BS" && !a.Metodo.Contains("Efectivo")).Sum(a => a.Total);

                decimal fisicoUsd = arqueo.Where(a => (a.Moneda == "BCV" || a.Moneda == "USD") && (a.Metodo.Contains("Divisas") || a.Metodo.Contains("Efectivo"))).Sum(a => a.Total);
                decimal bancoUsd = arqueo.Where(a => (a.Moneda == "BCV" || a.Moneda == "USD") && a.Metodo.Contains("Zelle")).Sum(a => a.Total);

                txtFisicoBs.Text = $"Físico (Efectivo): {fisicoBs:N2} BS";
                txtBancoBs.Text = $"Banco (PM/Transf): {bancoBs:N2} BS";
                txtTotalBs.Text = $"TOTAL BS: {fisicoBs + bancoBs:N2} BS";

                txtFisicoUsd.Text = $"Físico (Billetes): {fisicoUsd:N2} $";
                txtBancoUsd.Text = $"Banco (Zelle): {bancoUsd:N2} $";
                txtTotalUsd.Text = $"TOTAL $: {fisicoUsd + bancoUsd:N2} $";
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError("Error al cargar arqueo: " + ex.Message);
            }
        }

        private void BtnConfirmarCierre_Click(object sender, RoutedEventArgs e)
        {
            if (AlertaWindow.SolicitarConfirmacion("¿Desea cerrar la caja definitivamente por el día de hoy?", "Cierre de Caja"))
            {
                try
                {
                    _cajaService.CerrarCaja();
                    CajaCerradaExitosamente = true;
                    AlertaWindow.MostrarExito("La caja ha sido cerrada correctamente.");

                    // RESPALDO AUTOMÁTICO POST-CIERRE
                    if (AlertaWindow.SolicitarConfirmacion("¿Desea realizar el respaldo de seguridad de la base de datos ahora?", "Respaldo Recomendado"))
                    {
                        var saveDialog = new SaveFileDialog
                        {
                            Title = "Guardar Respaldo",
                            FileName = $"Respaldo_Cierre_{DateTime.Now:yyyyMMdd}.sql",
                            Filter = "Archivo SQL (*.sql)|*.sql"
                        };

                        if (saveDialog.ShowDialog() == true)
                        {
                            _mantenimientoService.GenerarRespaldo(saveDialog.FileName);
                            AlertaWindow.MostrarExito("Respaldo guardado exitosamente. Puede retirarse.");
                        }
                    }

                    this.Close();
                }
                catch (Exception ex)
                {
                    AlertaWindow.MostrarError(ex.Message);
                }
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) { this.Close(); }
    }
}