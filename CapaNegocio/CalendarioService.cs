using System;
using System.Collections.Generic;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    public class CalendarioService
    {
        private readonly CalendarioRepository _repository = new CalendarioRepository();
        private readonly AuditorService _auditoria = new AuditorService(); // NUEVO

        public List<ClaseProgramada> ObtenerPorGrupo(int grupoId) => _repository.ObtenerPorGrupo(grupoId);

        public void CambiarEstado(int claseId, string nuevoEstado)
        {
            try
            {
                _repository.ActualizarEstado(claseId, nuevoEstado);
                // NUEVO: ¡Auditoría estricta para cambios que afectan la morosidad!
                _auditoria.Registrar("UPDATE", "CalendarioClases", claseId.ToString(), null, $"Estado de la clase cambiado a: {nuevoEstado}", true);
            }
            catch (Exception ex)
            {
                _auditoria.Registrar("UPDATE", "CalendarioClases", claseId.ToString(), null, $"Error: {ex.Message}", false);
                throw;
            }
        }

        public void GenerarClasesAutomaticas(int grupoId)
        {
            var (semanas, fechaInicio) = _repository.ObtenerDatosParaGeneracion(grupoId);
            if (semanas == 0) throw new InvalidOperationException("No se encontraron los datos del curso.");

            DateTime fechaActual = fechaInicio;

            for (int i = 0; i < semanas; i++)
            {
                if (fechaActual.DayOfWeek == DayOfWeek.Sunday)
                    fechaActual = fechaActual.AddDays(1);

                _repository.Agregar(new ClaseProgramada { GrupoId = grupoId, FechaClase = fechaActual });
                fechaActual = fechaActual.AddDays(7);
            }

            // NUEVO: Dejamos rastro de que se generó un calendario
            _auditoria.Registrar("INSERT", "CalendarioClases", null, null, $"Generadas {semanas} clases para el Grupo ID: {grupoId}", true);
        }
    }
}