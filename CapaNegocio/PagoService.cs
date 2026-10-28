using System;
using System.Collections.Generic;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    public class PagoService
    {
        private readonly PagoRepository _repository = new PagoRepository();
        private readonly AuditorService _auditoria = new AuditorService();

        public List<PagoDetalle> ObtenerDetalles(int pagoId) => _repository.ObtenerDetalles(pagoId);
        public List<Pago> ObtenerPagos(int limite = 100, string filtro = "") => _repository.ObtenerRecientes(limite, filtro);
        public decimal ObtenerTotalHoy(DateTime fechaAperturaCaja) => _repository.ObtenerTotalHoy(fechaAperturaCaja);
        public decimal ObtenerIngresosTotalesUSD()
        {
            return _repository.ObtenerIngresosTotalesUSD();
        }

        public void RegistrarPago(string numeroRecibo, string estudianteCedula, int inscripcionId, int conceptoId, decimal total, string monedaCodigo, DateTime fechaVencimiento, List<PagoDetalle> detalles)
        {
            try
            {
                decimal sumaBs = 0;
                foreach (var d in detalles) sumaBs += d.MontoEnVes;

                var pago = new Pago(numeroRecibo, estudianteCedula, inscripcionId, conceptoId, total, monedaCodigo, SesionService.Instancia.UsuarioActual?.Username ?? "desconocido", fechaVencimiento)
                { TotalEnVes = sumaBs };

                _repository.Agregar(pago, detalles);
                _auditoria.Registrar("INSERT", "Pagos", numeroRecibo, null, $"Recibo:{numeroRecibo}, Cédula:{estudianteCedula}, Total:{total}, Moneda:{monedaCodigo}", true);
            }
            catch (Exception)
            {
                _auditoria.Registrar("INSERT", "Pagos", numeroRecibo, null, $"Recibo:{numeroRecibo}, Cédula:{estudianteCedula}, Total:{total}", false);
                throw;
            }
        }
    }
}