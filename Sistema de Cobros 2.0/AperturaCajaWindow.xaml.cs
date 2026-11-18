using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class AperturaCajaWindow : Window
    {
        private readonly CajaService _cajaService = new CajaService();
        public bool CajaAbiertaExitosamente { get; private set; } = false;

        public AperturaCajaWindow()
        {
            InitializeComponent();
        }

        private void BtnAbrir_Click(object sender, RoutedEventArgs e)
        {
            var cultura = new CultureInfo("es-VE");
            decimal.TryParse(txtSaldoBs.Text.Trim(), NumberStyles.Any, cultura, out decimal saldoBs);
            decimal.TryParse(txtSaldoUsd.Text.Trim(), NumberStyles.Any, cultura, out decimal saldoUsd);

            if (saldoBs < 0 || saldoUsd < 0)
            {
                AlertaWindow.MostrarAdvertencia("Los montos no pueden ser negativos.");
                return;
            }

            try
            {
                _cajaService.AbrirCajaBimoneda(saldoBs, saldoUsd);
                CajaAbiertaExitosamente = true;
                this.Close();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError(ex.Message);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}