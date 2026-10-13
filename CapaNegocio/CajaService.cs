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

        public void AbrirCaja(decimal saldoInicial)
        {
            if (ObtenerCajaAbierta() != null)
                throw new InvalidOperationException("Ya existe una caja abierta. Debe cerrarla primero.");

            string usuario = SesionService.Instancia.UsuarioActual?.Username ?? "Desconocido";
            var caja = new Caja(usuario, saldoInicial, "VES");

            _repository.AbrirCaja(caja);
            _auditoria.Registrar("APERTURA", "Caja", null, null, $"Saldo Inicial: {saldoInicial} VES", true);
        }

        public void CerrarCaja()
        {
            var caja = ObtenerCajaAbierta();
            if (caja == null)
                throw new InvalidOperationException("No hay ninguna caja abierta para cerrar.");

            string usuario = SesionService.Instancia.UsuarioActual?.Username ?? "Desconocido";

            // Nota: En una fase más avanzada se calculará el saldo exacto cruzando Pagos vs Devoluciones
            // Por ahora, cerramos con el saldo declarado como 0 hasta implementar el arqueo.
            _repository.CerrarCaja(caja.Id, usuario, 0);

            _auditoria.Registrar("CIERRE", "Caja", caja.Id.ToString(), null, $"Caja Cerrada por {usuario}", true);
        }
    }
}
