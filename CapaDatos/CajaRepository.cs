using System;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    public class CajaRepository
    {
        public Caja? ObtenerCajaAbierta()
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT * FROM Caja WHERE Estado = 'Abierta' ORDER BY FechaApertura DESC LIMIT 1", conn);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Caja(
                    reader.GetString("UsuarioApertura"),
                    reader.GetDecimal("SaldoInicial"),
                    reader.GetString("SaldoInicialMoneda")
                )
                {
                    Id = reader.GetInt32("Id"),
                    FechaApertura = reader.GetDateTime("FechaApertura"),
                    Estado = reader.GetString("Estado")
                };
            }
            return null;
        }

        public void AbrirCaja(Caja caja)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "INSERT INTO Caja (UsuarioApertura, SaldoInicial, SaldoInicialMoneda, Estado, FechaApertura) " +
                "VALUES (@usuario, @saldo, @moneda, 'Abierta', @fecha)", conn);
            cmd.Parameters.AddWithValue("@usuario", caja.UsuarioApertura);
            cmd.Parameters.AddWithValue("@saldo", caja.SaldoInicial);
            cmd.Parameters.AddWithValue("@moneda", caja.SaldoInicialMoneda);
            cmd.Parameters.AddWithValue("@fecha", caja.FechaApertura);
            cmd.ExecuteNonQuery();
        }

        public void CerrarCaja(int cajaId, string usuarioCierre, decimal saldoFinal)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "UPDATE Caja SET Estado = 'Cerrada', FechaCierre = @fecha, UsuarioCierre = @usuario, SaldoFinal = @saldo " +
                "WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@fecha", DateTime.Now);
            cmd.Parameters.AddWithValue("@usuario", usuarioCierre);
            cmd.Parameters.AddWithValue("@saldo", saldoFinal);
            cmd.Parameters.AddWithValue("@id", cajaId);
            cmd.ExecuteNonQuery();
        }
    }
}