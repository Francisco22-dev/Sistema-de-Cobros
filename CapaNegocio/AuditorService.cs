using System.Collections.Generic;
using System.Net;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    /// <summary>
    /// Servicio de auditoría del sistema. Registra con IP y computadora.
    /// </summary>
    public class AuditorService
    {
        private readonly AuditoriaRepository _repository;

        public AuditorService()
        {
            _repository = new AuditoriaRepository();
        }

        public void Registrar(string accion, string tablaAfectada, string? registroId,
            string? datosAnteriores, string? datosNuevos, bool exito)
        {
            var auditoria = new Auditoria(
                SesionService.Instancia.UsuarioActual?.Username ?? "desconocido",
                accion, tablaAfectada, datosAnteriores, datosNuevos,
                DatabaseConfig.NombreComputadora, ObtenerIpLocal(), exito);
            _repository.Registrar(auditoria);
        }

        public List<Auditoria> ObtenerRegistros(int limite = 100)
        {
            return _repository.ObtenerRecientes(limite);
        }

        private static string ObtenerIpLocal()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return ip.ToString();
                }
                return "127.0.0.1";
            }
            catch
            {
                return "desconocida";
            }
        }
    }
}
