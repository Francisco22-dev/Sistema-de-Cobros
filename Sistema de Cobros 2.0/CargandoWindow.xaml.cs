using System;
using System.Threading.Tasks;
using System.Windows;

namespace Sistema_de_Cobros_2._0
{
    public partial class CargandoWindow : Window
    {
        public CargandoWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await IniciarProcesoCargaAsync();
        }

        private async Task IniciarProcesoCargaAsync()
        {
            try
            {
                // Paso 1: Conexión con MariaDB
                ActualizarProgreso("Conectando con el servidor local...", "Verificando enlace de red y credenciales...", 25);
                await Task.Run(() =>
                {
                    CapaDatos.DatabaseConnection.VerificarConexion();
                });

                // Paso 2: Sincronizar Tasas de Cambio y Caja
                ActualizarProgreso("Sincronizando tasa de cambio y caja diaria...", "Comprobando apertura de turno...", 55);
                await Task.Run(() =>
                {
                    var tasaService = new CapaNegocio.TasaCambioService();
                    tasaService.ObtenerTasasDelDia();

                    var cajaService = new CapaNegocio.CajaService();
                    cajaService.ObtenerCajaAbierta();
                });

                // Paso 3: Precargar catálogos principales
                ActualizarProgreso("Sincronizando estudiantes y cursos...", "Preparando interfaz de facturación...", 85);
                await Task.Run(() =>
                {
                    var inscripcionService = new CapaNegocio.InscripcionService();
                    inscripcionService.ObtenerInscripcionesActivas();
                });

                // Paso 4: Finalizar
                ActualizarProgreso("Listo para iniciar", "Abriendo pantalla principal...", 100);
                await Task.Delay(350); // Pausa visual suave

                // Abrir pantalla principal sin retrasos
                var mainWindow = new MainWindow();
                mainWindow.Show();

                this.Close();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al inicializar el sistema:\n{ex.Message}");
                // Si la base de datos no responde, volver al login
                new LoginWindow().Show();
                this.Close();
            }
        }

        private void ActualizarProgreso(string estado, string detalle, double porcentaje)
        {
            txtEstado.Text = estado;
            txtDetalle.Text = detalle;
            pbCarga.Value = porcentaje;
        }
    }
}