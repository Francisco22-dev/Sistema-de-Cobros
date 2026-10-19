using System;
using System.Collections.Generic;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    public class CursoService
    {
        private readonly CursoRepository _repository = new CursoRepository();
        private readonly AuditorService _auditoria = new AuditorService();

        public List<Curso> ObtenerCursos() => _repository.ObtenerTodos();

        public void GuardarCurso(string codigo, string nombre, int meses, decimal precioInscripcion, decimal precioClase)
        {
            int semanasAutomáticas = meses * 4;

            var curso = new Curso(codigo, nombre, semanasAutomáticas)
            {
                PrecioInscripcion = precioInscripcion,
                PrecioMensualidad = precioClase
            };

            try
            {
                _repository.Agregar(curso);
                _auditoria.Registrar("INSERT", "Cursos", codigo, null, $"Duración: {meses} meses ({semanasAutomáticas} clases), Precio Clase: {precioClase}", true);
            }
            catch (Exception ex)
            {
                _auditoria.Registrar("INSERT", "Cursos", codigo, null, ex.Message, false);
                throw;
            }
        }

        public void ActualizarCurso(Curso curso)
        {
            // CORRECCIÓN AUDITORÍA: Buscamos cómo estaba antes
            var cursoAntiguo = _repository.ObtenerTodos().Find(c => c.Codigo == curso.Codigo);
            string datosViejos = cursoAntiguo != null ? $"Precio Clase: {cursoAntiguo.PrecioMensualidad}, Activo: {cursoAntiguo.Activo}" : "Desconocido";

            curso.DuracionSemanas = (curso.DuracionSemanas / 4) * 4;
            _repository.Actualizar(curso);

            // Ahora sí enviamos los datos viejos
            _auditoria.Registrar("UPDATE", "Cursos", curso.Codigo, datosViejos, $"Precio Clase: {curso.PrecioMensualidad}, Activo: {curso.Activo}", true);
        }

        public void CambiarEstadoCurso(string codigo, bool nuevoEstado)
        {
            var curso = _repository.ObtenerTodos().Find(c => c.Codigo == codigo);
            if (curso != null)
            {
                // CORRECCIÓN AUDITORÍA: Guardamos el estado anterior
                string datosViejos = $"Activo: {curso.Activo}";
                curso.Activo = nuevoEstado;
                _repository.Actualizar(curso);

                _auditoria.Registrar("UPDATE", "Cursos", codigo, datosViejos, $"Activo cambiado a {nuevoEstado}", true);
            }
        }
    }
}