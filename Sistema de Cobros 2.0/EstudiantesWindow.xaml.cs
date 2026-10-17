using System;
using System.Windows;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class EstudiantesWindow : Window
    {
        private readonly EstudianteService _estudianteService = new EstudianteService();
        private readonly InscripcionService _inscripcionService = new InscripcionService();

        public EstudiantesWindow()
        {
            InitializeComponent();
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string cedula = txtCedula.Text.Trim();
            string nombre = txtNombre.Text.Trim();
            string telefono = txtTelefono.Text.Trim();

            if (string.IsNullOrWhiteSpace(cedula) || string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(telefono))
            {
                AlertaWindow.MostrarAdvertencia("Por favor complete todos los campos (Cédula, Nombre y Teléfono).");
                return;
            }

            try
            {
                // 1. Crear estudiante
                _estudianteService.CrearEstudiante(cedula, nombre, telefono, null, null);

                // 2. Inscribirlo automáticamente en el Grupo 1 (Para que aparezca en el cobro)
                _inscripcionService.RegistrarInscripcion(cedula, 1, "Inscripción automática");

                AlertaWindow.MostrarExito("Estudiante registrado e inscrito exitosamente.");
                this.Close();
            }
            catch (Exception ex)
            {
                AlertaWindow.MostrarError($"Error al registrar: {ex.Message}");
            }
        }
    }
}