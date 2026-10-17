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
        public System.Collections.Generic.List<ArqueoItem> ObtenerArqueoCaja(DateTime fechaApertura)
        {
            var lista = new System.Collections.Generic.List<ArqueoItem>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();

            // Sumamos los detalles de los pagos VIGENTES realizados desde que se abrió esta caja
            using var cmd = new MySqlCommand(
                "SELECT mp.Nombre AS Metodo, pd.MonedaCodigo AS Moneda, SUM(pd.Monto) AS Total " +
                "FROM PagoDetalles pd " +
                "JOIN Pagos p ON pd.PagoId = p.Id " +
                "JOIN MetodosPago mp ON pd.MetodoPagoId = mp.Id " +
                "WHERE p.FechaPago >= @fechaApertura AND p.Estado = 'Vigente' " +
                "GROUP BY mp.Nombre, pd.MonedaCodigo " +
                "ORDER BY pd.MonedaCodigo, mp.Nombre", conn);

            cmd.Parameters.AddWithValue("@fechaApertura", fechaApertura);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new ArqueoItem
                {
                    Metodo = reader.GetString("Metodo"),
                    Moneda = reader.GetString("Moneda"),
                    Total = reader.GetDecimal("Total")
                });
            }
            return lista;
        }
    }
}