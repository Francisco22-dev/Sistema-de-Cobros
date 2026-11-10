using System;
using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    public class InscripcionRepository
    {
        public List<InscripcionInfo> ObtenerInscripcionesActivas()
        {
            var lista = new List<InscripcionInfo>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "SELECT i.Id, e.Cedula, e.NombreCompleto, e.Telefono, c.Nombre AS Curso, COALESCE(g.Nombre, 'Sin Horario') AS Horario, i.Estado, i.Observaciones " +
                "FROM Inscripciones i " +
                "JOIN Estudiantes e ON i.EstudianteCedula = e.Cedula " +
                "JOIN Grupos g ON i.GrupoId = g.Id " +
                "JOIN Cursos c ON g.CursoCodigo = c.Codigo " +
                "WHERE i.Estado = 'Activa' " +
                "ORDER BY e.NombreCompleto", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new InscripcionInfo
                {
                    InscripcionId = reader.GetInt32("Id"),
                    Cedula = reader.GetString("Cedula"),
                    NombreCompleto = reader.GetString("NombreCompleto"),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? "" : reader.GetString("Telefono"),
                    CursoNombre = reader.GetString("Curso"),
                    GrupoNombre = reader.GetString("Horario"),
                    Estado = reader.GetString("Estado"),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? "" : reader.GetString("Observaciones")
                });
            }
            return lista;
        }

        public List<InscripcionInfo> ObtenerTodasLasInscripciones(string filtro = "", string filtroEstado = "")
        {
            var lista = new List<InscripcionInfo>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();

            string query =
                "SELECT i.Id, e.Cedula, e.NombreCompleto, e.Telefono, c.Nombre AS Curso, COALESCE(g.Nombre, 'Sin Horario') AS Horario, i.Estado, i.Observaciones " +
                "FROM Inscripciones i " +
                "JOIN Estudiantes e ON i.EstudianteCedula = e.Cedula " +
                "JOIN Grupos g ON i.GrupoId = g.Id " +
                "JOIN Cursos c ON g.CursoCodigo = c.Codigo " +
                "WHERE 1=1 ";

            if (!string.IsNullOrWhiteSpace(filtro))
                query += "AND (e.Cedula LIKE @filtro OR e.NombreCompleto LIKE @filtro OR c.Nombre LIKE @filtro) ";

            if (!string.IsNullOrWhiteSpace(filtroEstado) && filtroEstado != "Todos")
                query += "AND i.Estado = @estado ";

            query += "ORDER BY i.Id DESC LIMIT 200";

            using var cmd = new MySqlCommand(query, conn);
            if (!string.IsNullOrWhiteSpace(filtro))
                cmd.Parameters.AddWithValue("@filtro", $"%{filtro}%");
            if (!string.IsNullOrWhiteSpace(filtroEstado) && filtroEstado != "Todos")
                cmd.Parameters.AddWithValue("@estado", filtroEstado);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new InscripcionInfo
                {
                    InscripcionId = reader.GetInt32("Id"),
                    Cedula = reader.IsDBNull(reader.GetOrdinal("Cedula")) ? "" : reader.GetString("Cedula"),
                    NombreCompleto = reader.IsDBNull(reader.GetOrdinal("NombreCompleto")) ? "" : reader.GetString("NombreCompleto"),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? "" : reader.GetString("Telefono"),
                    CursoNombre = reader.IsDBNull(reader.GetOrdinal("Curso")) ? "" : reader.GetString("Curso"),
                    GrupoNombre = reader.IsDBNull(reader.GetOrdinal("Horario")) ? "Sin Horario" : reader.GetString("Horario"),
                    Estado = reader.IsDBNull(reader.GetOrdinal("Estado")) ? "Activa" : reader.GetString("Estado"),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? "" : reader.GetString("Observaciones")
                });
            }
            return lista;
        }

        public void Agregar(Inscripcion inscripcion)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "INSERT INTO Inscripciones (EstudianteCedula, GrupoId, Estado, Observaciones) " +
                "VALUES (@cedula, @grupoId, @estado, @obs)", conn);
            cmd.Parameters.AddWithValue("@cedula", inscripcion.EstudianteCedula);
            cmd.Parameters.AddWithValue("@grupoId", inscripcion.GrupoId);
            cmd.Parameters.AddWithValue("@estado", inscripcion.Estado);
            cmd.Parameters.AddWithValue("@obs", inscripcion.Observaciones ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void ActualizarEstado(int inscripcionId, string nuevoEstado, string? motivo)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "UPDATE Inscripciones SET Estado = @estado, " +
                "Observaciones = IF(@motivo IS NOT NULL AND @motivo != '', @motivo, Observaciones) " +
                "WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@estado", nuevoEstado);
            cmd.Parameters.AddWithValue("@motivo", string.IsNullOrWhiteSpace(motivo) ? (object)DBNull.Value : motivo.Trim());
            cmd.Parameters.AddWithValue("@id", inscripcionId);
            cmd.ExecuteNonQuery();
        }

        public List<Inscripcion> ObtenerRecientes(int limite = 100)
        {
            var lista = new List<Inscripcion>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT * FROM Inscripciones ORDER BY Id DESC LIMIT @limite", conn);
            cmd.Parameters.AddWithValue("@limite", limite);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Inscripcion(reader.GetString("EstudianteCedula"), reader.GetInt32("GrupoId"))
                {
                    Id = reader.GetInt32("Id"),
                    FechaInscripcion = reader.GetDateTime("FechaInscripcion"),
                    Estado = reader.GetString("Estado"),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? null : reader.GetString("Observaciones")
                });
            }
            return lista;
        }
    }
}