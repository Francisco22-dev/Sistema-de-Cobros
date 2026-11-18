using System;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    public class CajaService
    {
        private readonly CajaRepository _repository;
        private readonly AuditorService _auditoria;

        public CajaService()
        {
            _repository = new CajaRepository();
            _auditoria = new AuditorService();
        }

        public Caja? ObtenerCajaAbierta()
        {
            return _repository.ObtenerCajaAbierta();
        }

        public void AbrirCajaBimoneda(decimal saldoBs, decimal saldoUsd)
        {
            if (ObtenerCajaAbierta() != null)
                throw new InvalidOperationException("Ya existe una caja abierta. Debe cerrarla primero.");

            string usuario = SesionService.Instancia.UsuarioActual?.Username ?? "Desconocido";
            var caja = new Caja(usuario, saldoBs, saldoUsd);

            _repository.AbrirCaja(caja);
            _auditoria.Registrar("APERTURA", "Caja", null, null, $"Fondo Inicial: {saldoBs:N2} Bs | {saldoUsd:N2} $", true);
        }

        // NUEVO: Obtener la lista de ingresos agrupados
        public System.Collections.Generic.List<ArqueoItem> GenerarArqueo()
        {
            var caja = ObtenerCajaAbierta();
            if (caja == null) throw new InvalidOperationException("No hay ninguna caja abierta.");

            return _repository.ObtenerArqueoCaja(caja.FechaApertura);
        }

        public void CerrarCaja()
        {
            var caja = ObtenerCajaAbierta();
            if (caja == null)
                throw new InvalidOperationException("No hay ninguna caja abierta para cerrar.");

            string usuario = SesionService.Instancia.UsuarioActual?.Username ?? "Desconocido";

            // Cerramos la caja con 0 como saldo final temporal, ya que el desglose real queda en Pagos
            _repository.CerrarCaja(caja.Id, usuario, 0);

            _auditoria.Registrar("CIERRE", "Caja", caja.Id.ToString(), null, $"Caja Cerrada por {usuario}", true);
        }
        
        public List<(string Curso, int Cantidad)> ObtenerInscripcionesDelTurno()
        {
            var caja = ObtenerCajaAbierta();
            if (caja == null) return new List<(string Curso, int Cantidad)>();
            return _repository.ObtenerInscripcionesPorCursoDelTurno(caja.FechaApertura);
        }
    }
}
