using System;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    public class TasaCambioRepository
    {
        // 1. Busca estrictamente la tasa de la fecha de hoy
        public TasaCambio? ObtenerTasaDeHoy(string origen, string destino)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "SELECT * FROM TasasCambio WHERE MonedaOrigen = @origen AND MonedaDestino = @destino AND Fecha = CURDATE()", conn);
            cmd.Parameters.AddWithValue("@origen", origen);
            cmd.Parameters.AddWithValue("@destino", destino);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new TasaCambio(
                    reader.GetDateTime("Fecha"), reader.GetString("MonedaOrigen"),
                    reader.GetString("MonedaDestino"), reader.GetDecimal("Tasa"),
                    reader.GetString("RegistradoPor"))
                {
                    Id = reader.GetInt32("Id")
                };
            }
            return null;
        }

        // 2. Busca la última tasa histórica registrada en caso de que hoy no se haya cargado
        public TasaCambio? ObtenerUltimaTasaRegistrada(string origen, string destino)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "SELECT * FROM TasasCambio WHERE MonedaOrigen = @origen AND MonedaDestino = @destino " +
                "ORDER BY Fecha DESC, Id DESC LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@origen", origen);
            cmd.Parameters.AddWithValue("@destino", destino);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new TasaCambio(
                    reader.GetDateTime("Fecha"), reader.GetString("MonedaOrigen"),
                    reader.GetString("MonedaDestino"), reader.GetDecimal("Tasa"),
                    reader.GetString("RegistradoPor"))
                {
                    Id = reader.GetInt32("Id")
                };
            }
            return null;
        }

        // 3. Registra o actualiza la tasa del día actual
        public void RegistrarOActualizar(TasaCambio tasa)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "INSERT INTO TasasCambio (Fecha, MonedaOrigen, MonedaDestino, Tasa, Fuente, RegistradoPor) " +
                "VALUES (@fecha, @origen, @destino, @tasa, @fuente, @usuario) " +
                "ON DUPLICATE KEY UPDATE Tasa = @tasa, RegistradoPor = @usuario, FechaRegistro = NOW()", conn);
            cmd.Parameters.AddWithValue("@fecha", tasa.Fecha);
            cmd.Parameters.AddWithValue("@origen", tasa.MonedaOrigen);
            cmd.Parameters.AddWithValue("@destino", tasa.MonedaDestino);
            cmd.Parameters.AddWithValue("@tasa", tasa.Tasa);
            cmd.Parameters.AddWithValue("@fuente", tasa.Fuente ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@usuario", tasa.RegistradoPor);
            cmd.ExecuteNonQuery();
        }
    }
}