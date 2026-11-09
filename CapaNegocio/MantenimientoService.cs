using System;
using CapaDatos;

namespace CapaNegocio
{
    public class MantenimientoService
    {
        private readonly MantenimientoRepository _repository = new MantenimientoRepository();
        private readonly AuditorService _auditoria = new AuditorService();

        public void RealizarMantenimiento()
        {
            _repository.OptimizarTablas();
            _auditoria.Registrar("MANTENIMIENTO", "Sistema Completo", null, null, "Optimización de tablas ejecutada", true);
        }

        public void GenerarRespaldo(string rutaArchivo)
        {
            _repository.RespaldarBaseDeDatos(rutaArchivo);
            _auditoria.Registrar("RESPALDO", "Base de Datos", null, null, $"Respaldo guardado en: {rutaArchivo}", true);
        }
    }
}