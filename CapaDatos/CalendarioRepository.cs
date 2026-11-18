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

            int numero = 1;
            while (reader.Read())
            {
                string? obs = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? null : reader.GetString("Observaciones");

                // Si no tiene nota, le asigna automáticamente "Clase 1", "Clase 2", etc.
                if (string.IsNullOrWhiteSpace(obs))
                {
                    obs = $"Clase {numero}";
                }

                lista.Add(new ClaseProgramada
                {
                    Id = reader.GetInt32("Id"),
                    GrupoId = reader.GetInt32("GrupoId"),
                    FechaClase = reader.GetDateTime("FechaClase"),
                    Estado = reader.GetString("Estado"),
                    Observaciones = obs
                });
                numero++;
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
        // Reemplaza o agrega este método en CapaDatos/CalendarioRepository.cs:

        public (int Impartidas, int Total, int Nulas, DateTime? ProximaFecha) ObtenerProgresoGrupoAutomatico(int grupoId)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();

            // 1. Total clases del curso
            int total = 0;
            using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM CalendarioClases WHERE GrupoId = @gId", conn))
            {
                cmd.Parameters.AddWithValue("@gId", grupoId);
                total = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 2. Clases que ya ocurrieron hasta hoy y no fueron nulas
            int impartidas = 0;
            using (var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM CalendarioClases WHERE GrupoId = @gId AND FechaClase <= CURDATE() AND Estado NOT IN ('Nula', 'Festivo')", conn))
            {
                cmd.Parameters.AddWithValue("@gId", grupoId);
                impartidas = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 3. Clases canceladas por falta de profesor
            int nulas = 0;
            using (var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM CalendarioClases WHERE GrupoId = @gId AND Estado = 'Nula'", conn))
            {
                cmd.Parameters.AddWithValue("@gId", grupoId);
                nulas = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 4. Próxima clase por impartir
            DateTime? proxima = null;
            using (var cmd = new MySqlCommand(
                "SELECT MIN(FechaClase) FROM CalendarioClases WHERE GrupoId = @gId AND FechaClase >= CURDATE() AND Estado NOT IN ('Nula', 'Festivo')", conn))
            {
                cmd.Parameters.AddWithValue("@gId", grupoId);
                var obj = cmd.ExecuteScalar();
                if (obj != null && obj != DBNull.Value) proxima = Convert.ToDateTime(obj);
            }

            return (impartidas, total, nulas, proxima);
        }

        public void AgregarClaseAlFinal(int grupoId, string motivo)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();

            // Obtener la última fecha de clase registrada para ese grupo
            DateTime ultimaFecha = DateTime.Now.Date;
            using (var cmdUltima = new MySqlCommand("SELECT MAX(FechaClase) FROM CalendarioClases WHERE GrupoId = @gId", conn))
            {
                cmdUltima.Parameters.AddWithValue("@gId", grupoId);
                var obj = cmdUltima.ExecuteScalar();
                if (obj != null && obj != DBNull.Value) ultimaFecha = Convert.ToDateTime(obj);
            }

            DateTime nuevaFecha = ultimaFecha.AddDays(7);

            using var cmd = new MySqlCommand(
                "INSERT INTO CalendarioClases (GrupoId, FechaClase, Estado, Observaciones) " +
                "VALUES (@gId, @fecha, 'Programada', @obs)", conn);
            cmd.Parameters.AddWithValue("@gId", grupoId);
            cmd.Parameters.AddWithValue("@fecha", nuevaFecha);
            cmd.Parameters.AddWithValue("@obs", $"Clase recuperativa: {motivo}");
            cmd.ExecuteNonQuery();
        }
    }
}