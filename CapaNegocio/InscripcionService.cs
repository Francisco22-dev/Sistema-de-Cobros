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

        public List<InscripcionInfo> ObtenerInscripcionesActivas() => _repository.ObtenerInscripcionesActivas();
        public List<InscripcionInfo> ObtenerTodasLasInscripciones(string filtro = "", string estado = "") => _repository.ObtenerTodasLasInscripciones(filtro, estado);

        public bool TieneDeudaPendiente(string estudianteCedula)
        {
            return _pagoRepository.TieneDeuda(estudianteCedula, DateTime.Now.Date);
        }

        public void TrasladarEstudianteDeGrupo(int inscripcionId, int nuevoGrupoId, string grupoAnteriorCodigo, string grupoNuevoCodigo, string observacionActual, string? motivo)
        {
            try
            {
                // Regla 3: Anexar nota histórica automática en Observaciones
                string notaTraslado = $"Trasladado de {grupoAnteriorCodigo} a {grupoNuevoCodigo} el {DateTime.Now:dd/MM/yyyy}." +
                                      (!string.IsNullOrWhiteSpace(motivo) ? $" Motivo: {motivo.Trim()}." : "");

                string observacionFinal = string.IsNullOrWhiteSpace(observacionActual)
                    ? notaTraslado
                    : $"{observacionActual} | {notaTraslado}";

                _repository.CambiarGrupo(inscripcionId, nuevoGrupoId, observacionFinal);

                _auditoria.Registrar("TRASLADO_GRUPO", "Inscripciones", inscripcionId.ToString(),
                    $"Grupo anterior: {grupoAnteriorCodigo}", $"Nuevo grupo: {grupoNuevoCodigo} | {notaTraslado}", true);
            }
            catch (Exception ex)
            {
                _auditoria.Registrar("TRASLADO_GRUPO", "Inscripciones", inscripcionId.ToString(), null, ex.Message, false);
                throw;
            }
        }

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
                if (TieneDeudaPendiente(estudianteCedula))
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