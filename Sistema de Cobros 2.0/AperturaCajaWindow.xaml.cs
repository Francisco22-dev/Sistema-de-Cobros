using System;
using System.Windows;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class AperturaCajaWindow : Window
    {
        private readonly CajaService _cajaService;
        public bool CajaAbiertaExitosamente { get; private set; } = false;

        public AperturaCajaWindow()
        {
            InitializeComponent();
            _cajaService = new CajaService();
            txtSaldoInicial.Focus();
        }

        private void BtnAbrir_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtSaldoInicial.Text, out decimal saldoInicial) && saldoInicial >= 0)
            {
                try
                {
                    _cajaService.AbrirCaja(saldoInicial);
                    CajaAbiertaExitosamente = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Por favor, ingrese un monto numérico válido mayor o igual a cero.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}