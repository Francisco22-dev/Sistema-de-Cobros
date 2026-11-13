using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.Generic;
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
            _listaCompleta = _morosidadService.ObtenerMorosos();
            dgMorosos.ItemsSource = _listaCompleta;
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
        private List<MorosidadInfo> _listaCompleta; // NUEVA VARIABLE GLOBAL

        private void TxtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_listaCompleta == null) return;

            string filtro = txtBuscar.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(filtro))
            {
                dgMorosos.ItemsSource = _listaCompleta;
                return;
            }

            // Busca simultáneamente en Cédula, Nombre, Curso completo, Código de Curso y Código de Grupo
            var filtrada = _listaCompleta.Where(m =>
                m.NombreCompleto.ToLower().Contains(filtro) ||
                m.Cedula.ToLower().Contains(filtro) ||
                m.Curso.ToLower().Contains(filtro) ||
                m.CursoCodigo.ToLower().Contains(filtro) ||
                m.GrupoCodigo.ToLower().Contains(filtro)
            ).ToList();

            dgMorosos.ItemsSource = filtrada;
        }
    }
}