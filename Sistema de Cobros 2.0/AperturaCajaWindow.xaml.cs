using System;
using System.Globalization;
using System.Windows;
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
            try
            {
                _cajaService.AbrirCaja(0); // El saldo inicial ya no se usa, pasa 0 por defecto
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