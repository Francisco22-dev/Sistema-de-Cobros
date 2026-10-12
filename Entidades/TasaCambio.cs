using System;

namespace Entidades
{
    /// <summary>
    /// Representa una tasa de cambio entre monedas.
    /// </summary>
    public class TasaCambio
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string MonedaOrigen { get; set; }
        public string MonedaDestino { get; set; }
        public decimal Tasa { get; set; }
        public string? Fuente { get; set; }
        public string RegistradoPor { get; set; }
        public DateTime FechaRegistro { get; set; }

        public TasaCambio(DateTime fecha, string monedaOrigen, string monedaDestino, decimal tasa, string registradoPor)
        {
            Fecha = fecha;
            MonedaOrigen = monedaOrigen;
            MonedaDestino = monedaDestino;
            Tasa = tasa;
            RegistradoPor = registradoPor;
            FechaRegistro = DateTime.Now;
        }
    }
}
