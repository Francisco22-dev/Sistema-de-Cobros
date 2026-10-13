using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    /// <summary>
    /// Repositorio de usuarios en MariaDB con anti-SQL injection.
    /// </summary>
    public class UsuarioRepository
    {
        public List<Usuario> ObtenerTodos()
        {
            var lista = new List<Usuario>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT * FROM Usuarios", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapearUsuario(reader));
            }
            return lista;
        }

        public void Agregar(Usuario usuario)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "INSERT INTO Usuarios (Username, PasswordHash, NombreCompleto, Rol, Email, Telefono, Activo) " +
                "VALUES (@username, @hash, @nombre, @rol, @email, @telefono, @activo)", conn);
            cmd.Parameters.AddWithValue("@username", usuario.Username);
            cmd.Parameters.AddWithValue("@hash", usuario.PasswordHash);
            cmd.Parameters.AddWithValue("@nombre", usuario.NombreCompleto);
            cmd.Parameters.AddWithValue("@rol", usuario.Rol);
            cmd.Parameters.AddWithValue("@email", usuario.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@telefono", usuario.Telefono ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@activo", usuario.Activo);
            cmd.ExecuteNonQuery();
        }

        public void Actualizar(Usuario usuario)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "UPDATE Usuarios SET NombreCompleto = @nombre, Rol = @rol, Activo = @activo, " +
                "PasswordHash = COALESCE(@hash, PasswordHash), Email = @email, Telefono = @telefono " +
                "WHERE Username = @username", conn);
            cmd.Parameters.AddWithValue("@username", usuario.Username);
            cmd.Parameters.AddWithValue("@nombre", usuario.NombreCompleto);
            cmd.Parameters.AddWithValue("@rol", usuario.Rol);
            cmd.Parameters.AddWithValue("@activo", usuario.Activo);
            cmd.Parameters.AddWithValue("@hash", string.IsNullOrEmpty(usuario.PasswordHash) ? (object)DBNull.Value : usuario.PasswordHash);
            cmd.Parameters.AddWithValue("@email", usuario.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@telefono", usuario.Telefono ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void Eliminar(string username)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("DELETE FROM Usuarios WHERE Username = @username", conn);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.ExecuteNonQuery();
        }

        private Usuario MapearUsuario(MySqlDataReader reader)
        {
            return new Usuario(
                reader.GetString("Username"),
                reader.GetString("PasswordHash"),
                reader.GetString("NombreCompleto"),
                reader.GetString("Rol"))
            {
                Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString("Email"),
                Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? null : reader.GetString("Telefono"),
                Activo = reader.GetBoolean("Activo")
            };
        }
    }
}
