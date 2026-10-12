using System;

namespace Entidades
{
    /// <summary>
    /// Representa un estudiante del instituto.
    /// </summary>
    public class Estudiante
    {
        public string Cedula { get; set; }
        public string NombreCompleto { get; set; }
        public string Telefono { get; set; }
        public string? Email { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string? Direccion { get; set; }
        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }

        public Estudiante(string cedula, string nombreCompleto, string telefono)
        {
            Cedula = cedula;
            NombreCompleto = nombreCompleto;
            Telefono = telefono;
            FechaRegistro = DateTime.Now;
            Activo = true;
        }
    }
}
