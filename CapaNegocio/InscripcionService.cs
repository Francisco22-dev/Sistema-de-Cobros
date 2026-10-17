using System;
using System.Collections.Generic;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    /// <summary>
    /// Servicio de negocio para la gestión de inscripciones y bloqueo por morosidad.
    /// </summary>
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

        public List<Inscripcion> ObtenerInscripciones(int limite = 100)
        {
            return _repository.ObtenerRecientes(limite);
        }
        public List<InscripcionInfo> ObtenerInscripcionesActivas()
        {
            return _repository.ObtenerInscripcionesActivas();
        }

        public void RegistrarInscripcion(string estudianteCedula, int grupoId, string? observaciones = null)
        {
            try
            {
                // REGLA DE NEGOCIO: Bloquear si tiene deudas
                if (_pagoRepository.TieneDeuda(estudianteCedula))
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