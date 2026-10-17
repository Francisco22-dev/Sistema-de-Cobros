using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    public class InscripcionRepository
    {
        public List<Inscripcion> ObtenerRecientes(int limite = 100)
        {
            var lista = new List<Inscripcion>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT * FROM Inscripciones ORDER BY FechaInscripcion DESC LIMIT @limite", conn);
            cmd.Parameters.AddWithValue("@limite", limite);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new Inscripcion(
                    reader.GetString("EstudianteCedula"),
                    reader.GetInt32("GrupoId"))
                {
                    Id = reader.GetInt32("Id"),
                    FechaInscripcion = reader.GetDateTime("FechaInscripcion"),
                    Estado = reader.GetString("Estado"),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? null : reader.GetString("Observaciones")
                });
            }
            return lista;
        }

        public void Agregar(Inscripcion inscripcion)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "INSERT INTO Inscripciones (EstudianteCedula, GrupoId, FechaInscripcion, Estado, Observaciones) " +
                "VALUES (@cedula, @grupo, @fecha, @estado, @obs)", conn);
            cmd.Parameters.AddWithValue("@cedula", inscripcion.EstudianteCedula);
            cmd.Parameters.AddWithValue("@grupo", inscripcion.GrupoId);
            cmd.Parameters.AddWithValue("@fecha", inscripcion.FechaInscripcion);
            cmd.Parameters.AddWithValue("@estado", inscripcion.Estado);
            cmd.Parameters.AddWithValue("@obs", inscripcion.Observaciones ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }
        public List<InscripcionInfo> ObtenerInscripcionesActivas()
        {
            var lista = new List<InscripcionInfo>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "SELECT i.Id AS InscripcionId, e.Cedula, e.NombreCompleto, g.Codigo AS GrupoCodigo " +
                "FROM Inscripciones i " +
                "JOIN Estudiantes e ON i.EstudianteCedula = e.Cedula " +
                "JOIN Grupos g ON i.GrupoId = g.Id " +
                "WHERE i.Estado = 'Activa'", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new InscripcionInfo
                {
                    InscripcionId = reader.GetInt32("InscripcionId"),
                    Cedula = reader.GetString("Cedula"),
                    NombreCompleto = reader.GetString("NombreCompleto"),
                    GrupoCodigo = reader.GetString("GrupoCodigo")
                });
            }
            return lista;
        }
    }
}