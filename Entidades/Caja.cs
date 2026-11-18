using System;

namespace Entidades
{
    public class Caja
    {
        public int Id { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public string UsuarioApertura { get; set; }
        public string? UsuarioCierre { get; set; }
        public decimal SaldoInicial { get; set; }
        public string SaldoInicialMoneda { get; set; }
        public string SaldoInicialMonedaBs { get; set; }
        public decimal SaldoInicialBs { get; set; }
        public decimal SaldoInicialUsd { get; set; }
        public decimal? SaldoFinal { get; set; }
        public string Estado { get; set; }

        public Caja(string usuarioApertura, decimal saldoBs, decimal saldoUsd)
        {
            UsuarioApertura = usuarioApertura;
            SaldoInicialBs = saldoBs;
            SaldoInicialUsd = saldoUsd;
            SaldoInicial = saldoUsd; 
            SaldoInicialMoneda = "USD";
            SaldoInicialMonedaBs = "BS";
            FechaApertura = DateTime.Now;
            Estado = "Abierta";
        }
    }
}