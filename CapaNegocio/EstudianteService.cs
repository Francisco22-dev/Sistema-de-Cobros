using System.Collections.Generic;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    /// <summary>
    /// Servicio de negocio para la gestión de estudiantes.
    /// </summary>
    public class EstudianteService
    {
        private readonly EstudianteRepository _repository;
        private readonly AuditorService _auditoria;

        public EstudianteService()
        {
            _repository = new EstudianteRepository();
            _auditoria = new AuditorService();
        }

        public List<Estudiante> ObtenerEstudiantes()
        {
            return _repository.ObtenerTodos();
        }

        public void CrearEstudiante(string cedula, string nombreCompleto, string telefono,
            string? email, string? direccion)
        {
            try
            {
                var estudiante = new Estudiante(cedula, nombreCompleto, telefono)
                {
                    Email = email,
                    Direccion = direccion
                };
                _repository.Agregar(estudiante);

                _auditoria.Registrar("INSERT", "Estudiantes", cedula, null,
                    $"Cédula:{cedula}, Nombre:{nombreCompleto}, Teléfono:{telefono}", true);
            }
            catch (Exception)
            {
                _auditoria.Registrar("INSERT", "Estudiantes", cedula, null,
                    $"Cédula:{cedula}, Nombre:{nombreCompleto}", false);
                throw;
            }
        }

        public void ActualizarEstudiante(Estudiante estudiante)
        {
            try
            {
                _repository.Actualizar(estudiante);
                _auditoria.Registrar("UPDATE", "Estudiantes", estudiante.Cedula, null,
                    $"Nombre:{estudiante.NombreCompleto}, Teléfono:{estudiante.Telefono}", true);
            }
            catch (Exception)
            {
                _auditoria.Registrar("UPDATE", "Estudiantes", estudiante.Cedula, null,
                    $"Nombre:{estudiante.NombreCompleto}", false);
                throw;
            }
        }

        public void DesactivarEstudiante(string cedula)
        {
            _repository.Desactivar(cedula);
            _auditoria.Registrar("DESACTIVAR", "Estudiantes", cedula, "Activo:True", "Activo:False", true);
        }
    }
}
