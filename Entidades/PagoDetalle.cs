namespace Entidades
{
    /// <summary>
    /// Representa un detalle de pago (para pagos mixtos).
    /// </summary>
    public class PagoDetalle
    {
        public int Id { get; set; }
        public int PagoId { get; set; }
        public int MetodoPagoId { get; set; }
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
