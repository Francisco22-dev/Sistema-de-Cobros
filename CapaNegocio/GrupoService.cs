using System;
using System.Collections.Generic;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    public class GrupoService
    {
        private readonly GrupoRepository _repository = new GrupoRepository();
        private readonly AuditorService _auditoria = new AuditorService();

        public List<Grupo> ObtenerPorCurso(string cursoCodigo) => _repository.ObtenerPorCurso(cursoCodigo);
        public List<GrupoInfo> ObtenerGruposActivos() => _repository.ObtenerGruposActivos();

        /// <summary>
        /// Sugiere un código con el formato de la academia (Ej: AAD2601, ASE2618)
        /// </summary>
        public string SugerirCodigoGrupo(string cursoCodigo)
        {
            int cuenta = _repository.ContarGruposDeCurso(cursoCodigo) + 1;
            string anioDosDigitos = DateTime.Now.ToString("yy");
            return $"{cursoCodigo.Trim().ToUpper()}{anioDosDigitos}{cuenta:D2}";
        }

        public void GuardarHorario(string cursoCodigo, string codigoGrupo, string nombreHorario, DateTime fechaInicio, decimal? precioInscripcion, decimal? precioClase)
        {
            codigoGrupo = codigoGrupo.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(codigoGrupo))
                throw new InvalidOperationException("Debe ingresar un código para el grupo.");

            if (_repository.ExisteCodigo(codigoGrupo))
                throw new InvalidOperationException($"El código de grupo '{codigoGrupo}' ya existe. Ingrese uno diferente.");

            var grupo = new Grupo(cursoCodigo, codigoGrupo, fechaInicio)
            {
                Nombre = nombreHorario,
                PrecioInscripcion = precioInscripcion,
                PrecioMensualidad = precioClase,
                Activo = true,
                Estado = "Planificado"
            };

            try
            {
                _repository.Agregar(grupo);
                _auditoria.Registrar("INSERT", "Grupos", codigoGrupo, null, $"Horario: {nombreHorario}, Inicio: {fechaInicio:d}, Precio Clase: {precioClase}", true);
            }
            catch (Exception ex)
            {
                _auditoria.Registrar("INSERT", "Grupos", codigoGrupo, null, ex.Message, false);
                throw;
            }
        }

        private readonly CalendarioRepository _calendarioRepo = new CalendarioRepository();

        public void ActualizarHorario(Grupo grupo, bool cambioFechaInicio)
        {
            _repository.Actualizar(grupo);

            // Si la fecha de inicio cambió, reajustar automáticamente todas las clases del calendario
            if (cambioFechaInicio)
            {
                _calendarioRepo.ReajustarFechasCalendario(grupo.Id, grupo.FechaInicio);
            }

            _auditoria.Registrar("UPDATE", "Grupos", grupo.Codigo, null,
                $"Horario: {grupo.Nombre}, Fecha Inicio: {grupo.FechaInicio:d} (Calendario reajustado: {(cambioFechaInicio ? "SÍ" : "NO")})", true);
        }
    }
}