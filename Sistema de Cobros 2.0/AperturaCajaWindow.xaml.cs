using System;
using System.Globalization;
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
            var formatoEstricto = new System.Globalization.NumberFormatInfo
            {
                NumberDecimalSeparator = ",",
                NumberGroupSeparator = "."
            };

            if (decimal.TryParse(txtSaldoInicial.Text.Trim(), System.Globalization.NumberStyles.Number, formatoEstricto, out decimal saldoInicial) && saldoInicial >= 0)
            {
                try
                {
                    _cajaService.AbrirCaja(saldoInicial);
                    CajaAbiertaExitosamente = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    AlertaWindow.MostrarError(ex.Message);
                }
            }
            else
            {
                AlertaWindow.MostrarAdvertencia("Ingrese un monto válido (Ej. 1.500,00).");
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}