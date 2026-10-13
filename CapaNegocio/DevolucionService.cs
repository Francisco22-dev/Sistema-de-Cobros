using System;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    public class DevolucionService
    {
        private readonly DevolucionRepository _repository;
        private readonly AuditorService _auditoria;
        private readonly CajaService _cajaService;

        public DevolucionService()
        {
            _repository = new DevolucionRepository();
            _auditoria = new AuditorService();
            _cajaService = new CajaService();
        }

        public void RealizarDevolucion(Pago pago, string motivo)
        {
            // REGLA 1: La caja debe estar abierta para sacar dinero
            if (_cajaService.ObtenerCajaAbierta() == null)
                throw new InvalidOperationException("No se puede procesar la devolución porque la caja está cerrada.");

            // REGLA 2: No devolver algo ya devuelto
            if (pago.Estado == "Devuelto" || pago.Estado == "Anulado")
                throw new InvalidOperationException("Este recibo ya ha sido devuelto o anulado previamente.");

            string usuario = SesionService.Instancia.UsuarioActual?.Username ?? "Desconocido";

            try
            {
                _repository.ProcesarDevolucion(pago.Id, pago.Total, pago.MonedaCodigo, pago.TotalEnVes, motivo, usuario);

                _auditoria.Registrar("DEVOLUCION", "Pagos y Devoluciones", pago.NumeroRecibo,
                    $"Estado: {pago.Estado}", $"Estado: Devuelto, Motivo: {motivo}", true);
            }
            catch (Exception ex)
            {
                _auditoria.Registrar("DEVOLUCION", "Pagos y Devoluciones", pago.NumeroRecibo, null,
                    $"Error: {ex.Message}", false);
                throw;
            }
        }
    }
}