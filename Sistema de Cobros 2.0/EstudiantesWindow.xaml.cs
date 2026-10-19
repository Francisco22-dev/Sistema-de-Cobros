using System;
using System.Windows;
using CapaNegocio;

namespace Sistema_de_Cobros_2._0
{
    public partial class EstudiantesWindow : Window
    {
        private readonly EstudianteService _estudianteService = new EstudianteService();
        private readonly InscripcionService _inscripcionService = new InscripcionService();
        private readonly GrupoService _grupoService = new GrupoService();

        public EstudiantesWindow()
        {
            InitializeComponent();
            CargarGrupos();
        }

        private void CargarGrupos()
        {
            var grupos = _grupoService.ObtenerGruposActivos();
            cmbGrupos.ItemsSource = grupos;
            if (grupos.Count > 0) cmbGrupos.SelectedIndex = 0;
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            string cedula = txtCedula.Text.Trim();
            string nombre = txtNombre.Text.Trim();
            string telefono = txtTelefono.Text.Trim();
            string email = txtEmail.Text.Trim();

            if (string.IsNullOrWhiteSpace(cedula) || string.IsNullOrWhiteSpace(nombre) || cmbGrupos.SelectedValue == null)
            {
                AlertaWindow.MostrarAdvertencia("Cédula, Nombre y Horario son obligatorios.");
                return;
            }

            int grupoId = (int)cmbGrupos.SelectedValue;

            try
            {
                _estudianteService.CrearEstudiante(cedula, nombre, telefono, email, null);
                _inscripcionService.RegistrarInscripcion(cedula, grupoId, "Inscripción en nuevo sistema");

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