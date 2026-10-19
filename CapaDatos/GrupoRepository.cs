using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    public class GrupoRepository
    {
        public List<Grupo> ObtenerPorCurso(string cursoCodigo)
        {
            var lista = new List<Grupo>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT * FROM Grupos WHERE CursoCodigo = @codigo ORDER BY Id DESC", conn);
            cmd.Parameters.AddWithValue("@codigo", cursoCodigo);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Grupo(reader.GetString("CursoCodigo"), reader.GetString("Codigo"), reader.GetDateTime("FechaInicio"))
                {
                    Id = reader.GetInt32("Id"),
                    Nombre = reader.IsDBNull(reader.GetOrdinal("Nombre")) ? "" : reader.GetString("Nombre"),
                    Estado = reader.GetString("Estado"),
                    Activo = reader.GetBoolean("Activo")
                });
            }
            return lista;
        }

        public List<GrupoInfo> ObtenerGruposActivos()
        {
            var lista = new List<GrupoInfo>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();

            // LA CORRECCIÓN ESTÁ AQUÍ: Usamos COALESCE para atrapar los NULL y poner "Sin Horario"
            using var cmd = new MySqlCommand(
                "SELECT g.Id, CONCAT(c.Nombre, ' (', COALESCE(g.Nombre, 'Sin Horario'), ')') AS Display " +
                "FROM Grupos g JOIN Cursos c ON g.CursoCodigo = c.Codigo " +
                "WHERE g.Activo = TRUE AND c.Activo = TRUE ORDER BY c.Nombre, g.Nombre", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new GrupoInfo { Id = reader.GetInt32("Id"), Display = reader.GetString("Display") });
            }
            return lista;
        }

        public void Agregar(Grupo grupo)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "INSERT INTO Grupos (CursoCodigo, Codigo, Nombre, FechaInicio, Estado, Activo) " +
                "VALUES (@curso, @codigo, @nombre, @inicio, @estado, @activo)", conn);
            cmd.Parameters.AddWithValue("@curso", grupo.CursoCodigo);
            cmd.Parameters.AddWithValue("@codigo", grupo.Codigo);
            cmd.Parameters.AddWithValue("@nombre", grupo.Nombre ?? (object)System.DBNull.Value);
            cmd.Parameters.AddWithValue("@inicio", grupo.FechaInicio);
            cmd.Parameters.AddWithValue("@estado", grupo.Estado);
            cmd.Parameters.AddWithValue("@activo", grupo.Activo);
            cmd.ExecuteNonQuery();
        }

        public int ContarGruposDeCurso(string cursoCodigo)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT COUNT(*) FROM Grupos WHERE CursoCodigo = @codigo", conn);
            cmd.Parameters.AddWithValue("@codigo", cursoCodigo);
            return System.Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}