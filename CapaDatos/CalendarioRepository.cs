using System;
using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    public class CalendarioRepository
    {
        public List<ClaseProgramada> ObtenerPorGrupo(int grupoId)
        {
            var lista = new List<ClaseProgramada>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT * FROM CalendarioClases WHERE GrupoId = @grupo ORDER BY FechaClase", conn);
            cmd.Parameters.AddWithValue("@grupo", grupoId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new ClaseProgramada
                {
                    Id = reader.GetInt32("Id"),
                    GrupoId = reader.GetInt32("GrupoId"),
                    FechaClase = reader.GetDateTime("FechaClase"),
                    Estado = reader.GetString("Estado"),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? null : reader.GetString("Observaciones")
                });
            }
            return lista;
        }

        public void Agregar(ClaseProgramada clase)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            // Usamos INSERT IGNORE para que si la fecha ya existe, no dé error por duplicado
            using var cmd = new MySqlCommand(
                "INSERT IGNORE INTO CalendarioClases (GrupoId, FechaClase, Estado, Observaciones) " +
                "VALUES (@grupo, @fecha, @estado, @obs)", conn);
            cmd.Parameters.AddWithValue("@grupo", clase.GrupoId);
            cmd.Parameters.AddWithValue("@fecha", clase.FechaClase);
            cmd.Parameters.AddWithValue("@estado", clase.Estado);
            cmd.Parameters.AddWithValue("@obs", clase.Observaciones ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void ActualizarEstado(int id, string nuevoEstado)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("UPDATE CalendarioClases SET Estado = @estado WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@estado", nuevoEstado);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public (int duracionSemanas, DateTime fechaInicio) ObtenerDatosParaGeneracion(int grupoId)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "SELECT c.DuracionSemanas, g.FechaInicio FROM Grupos g " +
                "JOIN Cursos c ON g.CursoCodigo = c.Codigo WHERE g.Id = @id", conn);
            cmd.Parameters.AddWithValue("@id", grupoId);
            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return (reader.GetInt32("DuracionSemanas"), reader.GetDateTime("FechaInicio"));
            return (0, DateTime.Now);
        }
    }
}