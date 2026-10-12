using System;

namespace Entidades
{
    /// <summary>
    /// Representa un movimiento de caja (ingreso o egreso).
    /// </summary>
    public class MovimientoCaja
    {
        public int Id { get; set; }
        public int CajaId { get; set; }
        public string Tipo { get; set; }
        public decimal Monto { get; set; }
        public string MonedaCodigo { get; set; }
        public string Concepto { get; set; }
        public string? Categoria { get; set; }
        public string? Referencia { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string UsuarioRegistra { get; set; }

        public MovimientoCaja(int cajaId, string tipo, decimal monto, string monedaCodigo,
            string concepto, string usuarioRegistra)
        {
            CajaId = cajaId;
            Tipo = tipo;
            Monto = monto;
            MonedaCodigo = monedaCodigo;
            Concepto = concepto;
            UsuarioRegistra = usuarioRegistra;
            FechaMovimiento = DateTime.Now;
        }
    }
}
