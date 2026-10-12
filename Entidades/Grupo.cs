using System;

namespace Entidades
{
    /// <summary>
    /// Representa un grupo/cohorte específico de un curso.
    /// </summary>
    public class Grupo
    {
        public int Id { get; set; }
        public string CursoCodigo { get; set; }
        public string Codigo { get; set; }
        public string? Nombre { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Estado { get; set; }
        public bool Activo { get; set; }

        public Grupo(string cursoCodigo, string codigo, DateTime fechaInicio)
        {
            CursoCodigo = cursoCodigo;
            Codigo = codigo;
            FechaInicio = fechaInicio;
            Estado = "Planificado";
            Activo = true;
        }
    }
}
