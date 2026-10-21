using System;

namespace Entidades
{
    public class ClaseProgramada
    {
        public int Id { get; set; }
        public int GrupoId { get; set; }
        public DateTime FechaClase { get; set; }
        public string Estado { get; set; } = "Programada"; // Programada, Realizada, Nula, Festivo
        public string? Observaciones { get; set; }
    }
}