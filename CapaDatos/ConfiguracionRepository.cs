using System;
using MySqlConnector;

namespace CapaDatos
{
    public class ConfiguracionRepository
    {
        public void InicializarTabla()
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "CREATE TABLE IF NOT EXISTS ConfiguracionSistema (" +
                "Clave VARCHAR(50) PRIMARY KEY, " +
                "Valor VARCHAR(255) NOT NULL)", conn);
            cmd.ExecuteNonQuery();
        }

        public string? ObtenerValor(string clave)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT Valor FROM ConfiguracionSistema WHERE Clave = @clave", conn);
            cmd.Parameters.AddWithValue("@clave", clave);
            var result = cmd.ExecuteScalar();
            return result?.ToString();
        }

        public void GuardarValor(string clave, string valor)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "INSERT INTO ConfiguracionSistema (Clave, Valor) VALUES (@clave, @valor) " +
                "ON DUPLICATE KEY UPDATE Valor = @valor", conn);
            cmd.Parameters.AddWithValue("@clave", clave);
            cmd.Parameters.AddWithValue("@valor", valor);
            cmd.ExecuteNonQuery();
        }
    }
}