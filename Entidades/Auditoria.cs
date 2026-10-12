using System;

namespace Entidades
{
    /// <summary>
    /// Representa un registro de auditoría del sistema.
    /// </summary>
    public class Auditoria
    {
        public int Id { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string TablaAfectada { get; set; } = string.Empty;
        public string? RegistroId { get; set; }
        public string? DatosAnteriores { get; set; }
        public string? DatosNuevos { get; set; }
        public string Computadora { get; set; } = string.Empty;
        public string? Ip { get; set; }
        public DateTime FechaHora { get; set; }
        public bool Exito { get; set; }

        public Auditoria() { }

        public Auditoria(string usuario, string accion, string tablaAfectada,
            string? datosAnteriores, string? datosNuevos, string computadora, string? ip, bool exito)
        {
            Usuario = usuario;
            Accion = accion;
            TablaAfectada = tablaAfectada;
            DatosAnteriores = datosAnteriores;
            DatosNuevos = datosNuevos;
            Computadora = computadora;
            Ip = ip;
            FechaHora = DateTime.Now;
            Exito = exito;
        }
    }
}
