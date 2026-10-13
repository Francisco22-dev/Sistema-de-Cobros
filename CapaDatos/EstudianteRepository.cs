using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    /// <summary>
    /// Repositorio de estudiantes en MariaDB con anti-SQL injection.
    /// </summary>
    public class EstudianteRepository
    {
        public List<Estudiante> ObtenerTodos()
        {
            var lista = new List<Estudiante>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT * FROM Estudiantes WHERE Activo = TRUE ORDER BY NombreCompleto", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapearEstudiante(reader));
            }
            return lista;
        }

        public Estudiante? ObtenerPorCedula(string cedula)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT * FROM Estudiantes WHERE Cedula = @cedula", conn);
            cmd.Parameters.AddWithValue("@cedula", cedula);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return MapearEstudiante(reader);
            return null;
        }

        public void Agregar(Estudiante estudiante)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "INSERT INTO Estudiantes (Cedula, NombreCompleto, Telefono, Email, FechaNacimiento, Direccion, Activo) " +
                "VALUES (@cedula, @nombre, @telefono, @email, @nacimiento, @direccion, @activo)", conn);
            cmd.Parameters.AddWithValue("@cedula", estudiante.Cedula);
            cmd.Parameters.AddWithValue("@nombre", estudiante.NombreCompleto);
            cmd.Parameters.AddWithValue("@telefono", estudiante.Telefono);
            cmd.Parameters.AddWithValue("@email", estudiante.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@nacimiento", estudiante.FechaNacimiento ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@direccion", estudiante.Direccion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@activo", estudiante.Activo);
            cmd.ExecuteNonQuery();
        }

        public void Actualizar(Estudiante estudiante)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "UPDATE Estudiantes SET NombreCompleto = @nombre, Telefono = @telefono, " +
                "Email = @email, FechaNacimiento = @nacimiento, Direccion = @direccion, Activo = @activo " +
                "WHERE Cedula = @cedula", conn);
            cmd.Parameters.AddWithValue("@cedula", estudiante.Cedula);
            cmd.Parameters.AddWithValue("@nombre", estudiante.NombreCompleto);
            cmd.Parameters.AddWithValue("@telefono", estudiante.Telefono);
            cmd.Parameters.AddWithValue("@email", estudiante.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@nacimiento", estudiante.FechaNacimiento ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@direccion", estudiante.Direccion ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@activo", estudiante.Activo);
            cmd.ExecuteNonQuery();
        }

        public void Desactivar(string cedula)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("UPDATE Estudiantes SET Activo = FALSE WHERE Cedula = @cedula", conn);
            cmd.Parameters.AddWithValue("@cedula", cedula);
            cmd.ExecuteNonQuery();
        }

        private Estudiante MapearEstudiante(MySqlDataReader reader)
        {
            return new Estudiante(
                reader.GetString("Cedula"),
                reader.GetString("NombreCompleto"),
                reader.GetString("Telefono"))
            {
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString("Email"),
                FechaNacimiento = reader.IsDBNull(reader.GetOrdinal("FechaNacimiento")) ? null : reader.GetDateTime("FechaNacimiento"),
                Direccion = reader.IsDBNull(reader.GetOrdinal("Direccion")) ? null : reader.GetString("Direccion"),
                FechaRegistro = reader.GetDateTime("FechaRegistro"),
                Activo = reader.GetBoolean("Activo")
            };
        }
    }
}
