namespace Entidades
{
    public class PagoDetalle
    {
        public int Id { get; set; }
        public int PagoId { get; set; }
        public int MetodoPagoId { get; set; }
        public string? MetodoNombre { get; set; } // NUEVO: Para saber si fue Zelle, Efectivo, etc.
        public decimal Monto { get; set; }
        public string MonedaCodigo { get; set; } = string.Empty;
        public decimal TasaCambio { get; set; }
        public decimal MontoEnVes { get; set; }
        public string? Referencia { get; set; }

        public PagoDetalle() { }

        public PagoDetalle(int pagoId, int metodoPagoId, decimal monto, string monedaCodigo,
            decimal tasaCambio, decimal montoEnVes, string? referencia = null)
        {
            PagoId = pagoId;
            MetodoPagoId = metodoPagoId;
            Monto = monto;
            MonedaCodigo = monedaCodigo;
            TasaCambio = tasaCambio;
            MontoEnVes = montoEnVes;
            Referencia = referencia;
        }
    }
}