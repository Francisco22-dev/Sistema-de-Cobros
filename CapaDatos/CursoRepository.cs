using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    /// <summary>
    /// Repositorio de cursos en MariaDB con anti-SQL injection.
    /// </summary>
    public class CursoRepository
    {
        public List<Curso> ObtenerTodos()
        {
            var lista = new List<Curso>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT * FROM Cursos WHERE Activo = TRUE ORDER BY Nombre", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Curso(
                    reader.GetString("Codigo"),
                    reader.GetString("Nombre"),
                    reader.GetInt32("DuracionSemanas"))
                {
                    Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion")) ? null : reader.GetString("Descripcion"),
                    PrecioInscripcion = reader.GetDecimal("PrecioInscripcion"),
                    PrecioMensualidad = reader.GetDecimal("PrecioMensualidad"),
                    Activo = reader.GetBoolean("Activo")
                });
            }
            return lista;
        }

        public void Agregar(Curso curso)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            conn.Open();
            using var cmd = new MySqlCommand(
                "INSERT INTO Cursos (Codigo, Nombre, Descripcion, DuracionSemanas, PrecioInscripcion, PrecioMensualidad, Activo) " +
                "VALUES (@codigo, @nombre, @desc, @duracion, @precioIns, @precioMen, @activo)", conn);
            cmd.Parameters.AddWithValue("@codigo", curso.Codigo);
            cmd.Parameters.AddWithValue("@nombre", curso.Nombre);
            cmd.Parameters.AddWithValue("@desc", curso.Descripcion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@duracion", curso.DuracionSemanas);
            cmd.Parameters.AddWithValue("@precioIns", curso.PrecioInscripcion);
            cmd.Parameters.AddWithValue("@precioMen", curso.PrecioMensualidad);
            cmd.Parameters.AddWithValue("@activo", curso.Activo);
            cmd.ExecuteNonQuery();
        }

        public void Actualizar(Curso curso)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            conn.Open();
            using var cmd = new MySqlCommand(
                "UPDATE Cursos SET Nombre = @nombre, Descripcion = @desc, DuracionSemanas = @duracion, " +
                "PrecioInscripcion = @precioIns, PrecioMensualidad = @precioMen, Activo = @activo WHERE Codigo = @codigo", conn);
            cmd.Parameters.AddWithValue("@codigo", curso.Codigo);
            cmd.Parameters.AddWithValue("@nombre", curso.Nombre);
            cmd.Parameters.AddWithValue("@desc", curso.Descripcion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@duracion", curso.DuracionSemanas);
            cmd.Parameters.AddWithValue("@precioIns", curso.PrecioInscripcion);
            cmd.Parameters.AddWithValue("@precioMen", curso.PrecioMensualidad);
            cmd.Parameters.AddWithValue("@activo", curso.Activo);
            cmd.ExecuteNonQuery();
        }

        public void Desactivar(string codigo)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            conn.Open();
            using var cmd = new MySqlCommand("UPDATE Cursos SET Activo = FALSE WHERE Codigo = @codigo", conn);
            cmd.Parameters.AddWithValue("@codigo", codigo);
            cmd.ExecuteNonQuery();
        }
    }
}
