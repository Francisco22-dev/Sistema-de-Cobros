using System;

namespace Entidades
{
    public class Pago
    {
        public int Id { get; set; }
        public string NumeroRecibo { get; set; } = string.Empty;
        public string EstudianteCedula { get; set; } = string.Empty;
        public int InscripcionId { get; set; }
        public int ConceptoId { get; set; }
        public DateTime FechaPago { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal Total { get; set; }
        public decimal TotalEnVes { get; set; }
        public string MonedaCodigo { get; set; } = string.Empty;
        public string UsuarioRegistra { get; set; } = string.Empty;
        public string Estado { get; set; } = "Vigente";
        public string? Observaciones { get; set; }

        // Propiedades de UI (Traídas por los JOINs)
        public string NombreEstudiante { get; set; } = string.Empty;
        public string CursoHorario { get; set; } = string.Empty;

        public Pago() { }

        public Pago(string numeroRecibo, string estudianteCedula, int inscripcionId, int conceptoId,
            decimal total, string monedaCodigo, string usuarioRegistra, DateTime fechaVencimiento)
        {
            NumeroRecibo = numeroRecibo;
            EstudianteCedula = estudianteCedula;
            InscripcionId = inscripcionId;
            ConceptoId = conceptoId;
            Total = total;
            MonedaCodigo = monedaCodigo;
            UsuarioRegistra = usuarioRegistra;
            FechaPago = DateTime.Now;
            FechaVencimiento = fechaVencimiento;
        }
    }
}