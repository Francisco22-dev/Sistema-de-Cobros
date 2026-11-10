using System;
using System.Collections.Generic;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    public class InscripcionService
    {
        private readonly InscripcionRepository _repository;
        private readonly PagoRepository _pagoRepository;
        private readonly AuditorService _auditoria;

        public InscripcionService()
        {
            _repository = new InscripcionRepository();
            _pagoRepository = new PagoRepository();
            _auditoria = new AuditorService();
        }

        public List<Inscripcion> ObtenerInscripciones(int limite = 100) => _repository.ObtenerRecientes(limite);
        public List<InscripcionInfo> ObtenerInscripcionesActivas() => _repository.ObtenerInscripcionesActivas();
        public List<InscripcionInfo> ObtenerTodasLasInscripciones(string filtro = "", string estado = "") => _repository.ObtenerTodasLasInscripciones(filtro, estado);

        public void CambiarEstadoInscripcion(int inscripcionId, string nuevoEstado, string? motivo)
        {
            try
            {
                _repository.ActualizarEstado(inscripcionId, nuevoEstado, motivo);
                _auditoria.Registrar("UPDATE_ESTADO", "Inscripciones", inscripcionId.ToString(), null,
                    $"Estado: {nuevoEstado} | Motivo: {motivo ?? "Sin motivo"}", true);
            }
            catch (Exception ex)
            {
                _auditoria.Registrar("UPDATE_ESTADO", "Inscripciones", inscripcionId.ToString(), null, ex.Message, false);
                throw;
            }
        }

        public void RegistrarInscripcion(string estudianteCedula, int grupoId, string? observaciones = null)
        {
            try
            {
                if (_pagoRepository.TieneDeuda(estudianteCedula, DateTime.Now.Date))
                {
                    throw new InvalidOperationException("No se puede inscribir: El estudiante tiene pagos vencidos pendientes.");
                }

                var inscripcion = new Inscripcion(estudianteCedula, grupoId)
                {
                    Observaciones = observaciones
                };

                _repository.Agregar(inscripcion);
                _auditoria.Registrar("INSERT", "Inscripciones", null, null,
                    $"Cédula:{estudianteCedula}, GrupoId:{grupoId}", true);
            }
            catch (Exception ex)
            {
                _auditoria.Registrar("INSERT", "Inscripciones", null, null,
                    $"Cédula:{estudianteCedula}, GrupoId:{grupoId} - Rechazado: {ex.Message}", false);
                throw;
            }
        }
    }
}