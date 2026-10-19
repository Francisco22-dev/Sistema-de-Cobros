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

        public void GuardarHorario(string cursoCodigo, string nombreHorario, DateTime fechaInicio)
        {
            int cuenta = _repository.ContarGruposDeCurso(cursoCodigo) + 1;
            string codigoGenerado = $"{cursoCodigo}-GRP-{cuenta:D3}"; // Ej: ING-01-GRP-001

            var grupo = new Grupo(cursoCodigo, codigoGenerado, fechaInicio)
            {
                Nombre = nombreHorario,
                Activo = true,
                Estado = "Planificado"
            };

            try
            {
                _repository.Agregar(grupo);
                _auditoria.Registrar("INSERT", "Grupos", codigoGenerado, null, $"Horario: {nombreHorario}, Inicio: {fechaInicio:d}", true);
            }
            catch (Exception ex)
            {
                _auditoria.Registrar("INSERT", "Grupos", codigoGenerado, null, ex.Message, false);
                throw;
            }
        }
    }
}