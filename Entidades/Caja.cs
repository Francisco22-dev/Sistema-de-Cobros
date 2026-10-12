using System;

namespace Entidades
{
    /// <summary>
    /// Representa la apertura y cierre de caja diaria.
    /// </summary>
    public class Caja
    {
        public int Id { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public string UsuarioApertura { get; set; }
        public string? UsuarioCierre { get; set; }
        public decimal SaldoInicial { get; set; }
        public string SaldoInicialMoneda { get; set; }
        public decimal? SaldoFinal { get; set; }
        public string Estado { get; set; }

        public Caja(string usuarioApertura, decimal saldoInicial, string saldoInicialMoneda)
        {
            UsuarioApertura = usuarioApertura;
            SaldoInicial = saldoInicial;
            SaldoInicialMoneda = saldoInicialMoneda;
            FechaApertura = DateTime.Now;
            Estado = "Abierta";
        }
    }
}
