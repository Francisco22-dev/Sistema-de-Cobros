using System;
using System.Collections.Generic;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    public class CalendarioService
    {
        private readonly CalendarioRepository _repository = new CalendarioRepository();

        public List<ClaseProgramada> ObtenerPorGrupo(int grupoId) => _repository.ObtenerPorGrupo(grupoId);

        public void CambiarEstado(int claseId, string nuevoEstado)
        {
            _repository.ActualizarEstado(claseId, nuevoEstado);
        }

        public void GenerarClasesAutomaticas(int grupoId)
        {
            var (semanas, fechaInicio) = _repository.ObtenerDatosParaGeneracion(grupoId);
            if (semanas == 0) throw new InvalidOperationException("No se encontraron los datos del curso.");

            DateTime fechaActual = fechaInicio;

            for (int i = 0; i < semanas; i++)
            {
                // Agregamos la clase, siempre y cuando no sea Domingo (si es domingo, sumamos 1 día al lunes)
                if (fechaActual.DayOfWeek == DayOfWeek.Sunday)
                    fechaActual = fechaActual.AddDays(1);

                _repository.Agregar(new ClaseProgramada { GrupoId = grupoId, FechaClase = fechaActual });

                // Sumamos 7 días para la clase de la siguiente semana
                fechaActual = fechaActual.AddDays(7);
            }
        }
    }
}