using System;
using System.Windows;
using Microsoft.Win32;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class MantenimientoWindow : Window
    {
        private readonly MantenimientoService _servicio = new MantenimientoService();

        public MantenimientoWindow() { InitializeComponent(); }

        private void BtnOptimizar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _servicio.RealizarMantenimiento();
                AlertaWindow.MostrarExito("Optimización completada. El sistema está funcionando al 100%.");
            }
            catch (Exception ex) { AlertaWindow.MostrarError(ex.Message); }
        }

        private void BtnRespaldar_Click(object sender, RoutedEventArgs e)
        {
            var saveDialog = new SaveFileDialog
            {
                Title = "Guardar Respaldo de Base de Datos",
                FileName = $"Respaldo_CEVENCA_{DateTime.Now:yyyyMMdd_HHmm}.sql",
                Filter = "Archivo SQL (*.sql)|*.sql"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    _servicio.GenerarRespaldo(saveDialog.FileName);
                    AlertaWindow.MostrarExito("¡Respaldo generado y guardado de manera segura!");
                }
                catch (Exception ex) { AlertaWindow.MostrarError(ex.Message); }
            }
        }
    }
}