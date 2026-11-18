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

        // ========================================================
        // ESTADOS VISUALES 100% AUTOMÁTICOS SEGÚN LA FECHA DE HOY
        // ========================================================
        public string EstadoVisual
        {
            get
            {
                if (Estado == "Nula") return "Faltó Profesor (NULA)";
                if (Estado == "Festivo") return "Feriado / Asueto";
                if (FechaClase.Date < DateTime.Now.Date) return "Impartida (Vista)";
                if (FechaClase.Date == DateTime.Now.Date) return "HOY (En Curso)";
                return "Programada (Futura)";
            }
        }

        public string ColorEstadoVisual
        {
            get
            {
                if (Estado == "Nula") return "#D32F2F"; // Rojo
                if (Estado == "Festivo") return "#757575"; // Gris
                if (FechaClase.Date < DateTime.Now.Date) return "#2E7D32"; // Verde
                if (FechaClase.Date == DateTime.Now.Date) return "#1565C0"; // Azul
                return "#555555"; // Gris oscuro
            }
        }
    }
}