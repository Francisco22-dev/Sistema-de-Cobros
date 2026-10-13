using System.Windows;

namespace Sistema_de_Cobros_2._0
{
    public partial class MotivoDevolucionWindow : Window
    {
        public string Motivo { get; private set; } = string.Empty;
        public bool Confirmado { get; private set; } = false;

        public MotivoDevolucionWindow()
        {
            InitializeComponent();
            txtMotivo.Focus();
        }

        private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMotivo.Text) || txtMotivo.Text.Length < 10)
            {
                MessageBox.Show("Debe ingresar un motivo válido y descriptivo (mínimo 10 caracteres).", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Motivo = txtMotivo.Text.Trim();
            Confirmado = true;
            this.Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}