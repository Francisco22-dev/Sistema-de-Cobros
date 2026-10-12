using System;

namespace Entidades
{
    /// <summary>
    /// Representa la inscripción de un estudiante en un grupo.
    /// </summary>
    public class Inscripcion
    {
        public int Id { get; set; }
        public string EstudianteCedula { get; set; }
        public int GrupoId { get; set; }
        public DateTime FechaInscripcion { get; set; }
        public string Estado { get; set; }
        public string? Observaciones { get; set; }

        public Inscripcion(string estudianteCedula, int grupoId)
        {
            EstudianteCedula = estudianteCedula;
            GrupoId = grupoId;
            FechaInscripcion = DateTime.Now;
            Estado = "Activa";
        }
    }
}
