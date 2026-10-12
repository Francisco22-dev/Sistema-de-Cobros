using System;
using System.Collections.Generic;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    /// <summary>
    /// Servicio de negocio para la gestión de pagos con transacciones y auditoría.
    /// </summary>
    public class PagoService
    {
        private readonly PagoRepository _repository;
        private readonly AuditorService _auditoria;

        public PagoService()
        {
            _repository = new PagoRepository();
            _auditoria = new AuditorService();
        }

        public List<Pago> ObtenerPagos()
        {
            return _repository.ObtenerTodos();
        }

        public void RegistrarPago(string numeroRecibo, string estudianteCedula, int inscripcionId,
            int conceptoId, decimal total, string monedaCodigo, DateTime fechaVencimiento,
            List<PagoDetalle> detalles)
        {
            try
            {
                var pago = new Pago(numeroRecibo, estudianteCedula, inscripcionId, conceptoId,
                    total, monedaCodigo,
                    SesionService.Instancia.UsuarioActual?.Username ?? "desconocido",
                    fechaVencimiento);

                _repository.Agregar(pago, detalles);

                _auditoria.Registrar("INSERT", "Pagos", numeroRecibo, null,
                    $"Recibo:{numeroRecibo}, Cédula:{estudianteCedula}, Total:{total}, Moneda:{monedaCodigo}", true);
            }
            catch (Exception)
            {
                _auditoria.Registrar("INSERT", "Pagos", numeroRecibo, null,
                    $"Recibo:{numeroRecibo}, Cédula:{estudianteCedula}, Total:{total}", false);
                throw;
            }
        }

        public decimal ObtenerTotalHoy()
        {
            return _repository.ObtenerTotalHoy();
        }
    }
}
