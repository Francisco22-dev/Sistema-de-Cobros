using System;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    public class TasaCambioRepository
    {
        public TasaCambio? ObtenerTasaDelDia(string origen, string destino)
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

        public void Registrar(TasaCambio tasa)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "INSERT INTO TasasCambio (Fecha, MonedaOrigen, MonedaDestino, Tasa, Fuente, RegistradoPor) " +
                "VALUES (@fecha, @origen, @destino, @tasa, @fuente, @usuario)", conn);
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