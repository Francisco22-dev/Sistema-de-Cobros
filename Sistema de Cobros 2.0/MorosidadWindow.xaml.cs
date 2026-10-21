using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;
using Entidades;

namespace Sistema_de_Cobros_2._0
{
    public partial class MorosidadWindow : Window
    {
        private readonly MorosidadService _morosidadService = new MorosidadService();

        public MorosidadWindow()
        {
            InitializeComponent();
            dgMorosos.ItemsSource = _morosidadService.ObtenerMorosos();
        }

        private void BtnWhatsApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MorosidadInfo moroso)
            {
                string telefonoWeb = _morosidadService.FormatearTelefonoWhatsapp(moroso.Telefono);
                string mensaje = $"Hola {moroso.NombreCompleto}, le escribimos de la Academia CEVENCA para saludarle. " +
                                 $"Queremos recordarle cordialmente que presenta un saldo pendiente de {moroso.DeudaStr} " +
                                 $"en su curso de {moroso.Curso}. Por favor, comuníquese con nosotros. ¡Gracias!";

                string url = $"https://web.whatsapp.com/send?phone={telefonoWeb}&text={System.Uri.EscapeDataString(mensaje)}";

                // Abre el navegador predeterminado de Windows
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
        }
    }
}