using Entidades;
using MySqlConnector;
using System;

namespace CapaDatos
{
    public class DevolucionRepository
    {
        public void ProcesarDevolucion(int pagoId, decimal monto, string monedaCodigo, decimal montoEnVes, string motivo, string usuarioRegistra)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Insertar el registro de la devolución
                using var cmdInsert = new MySqlCommand(
                    "INSERT INTO Devoluciones (PagoId, Monto, MonedaCodigo, TipoDevolucion, MontoEnVes, Motivo, UsuarioRegistra, Estado) " +
                    "VALUES (@pagoId, @monto, @moneda, 'Neta', @montoVes, @motivo, @usuario, 'Procesada')", conn, transaction);
                cmdInsert.Parameters.AddWithValue("@pagoId", pagoId);
                cmdInsert.Parameters.AddWithValue("@monto", monto);
                cmdInsert.Parameters.AddWithValue("@moneda", monedaCodigo);
                cmdInsert.Parameters.AddWithValue("@montoVes", montoEnVes);
                cmdInsert.Parameters.AddWithValue("@motivo", motivo);
                cmdInsert.Parameters.AddWithValue("@usuario", usuarioRegistra);
                cmdInsert.ExecuteNonQuery();

                // 2. Marcar el Pago original como 'Devuelto'
                using var cmdUpdate = new MySqlCommand(
                    "UPDATE Pagos SET Estado = 'Devuelto' WHERE Id = @pagoId", conn, transaction);
                cmdUpdate.Parameters.AddWithValue("@pagoId", pagoId);
                cmdUpdate.ExecuteNonQuery();

                // Confirmar transacción
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public Pago? ObtenerPagoPorRecibo(string numeroRecibo)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT * FROM Pagos WHERE NumeroRecibo = @recibo LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@recibo", numeroRecibo.Trim());
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Pago(
                    reader.GetString("NumeroRecibo"),
                    reader.GetString("EstudianteCedula"),
                    reader.GetInt32("InscripcionId"),
                    reader.GetInt32("ConceptoId"),
                    reader.GetDecimal("Total"),
                    reader.GetString("MonedaCodigo"),
                    reader.GetString("UsuarioRegistra"),
                    reader.GetDateTime("FechaVencimiento"))
                {
                    Id = reader.GetInt32("Id"),
                    TotalEnVes = reader.GetDecimal("TotalEnVes"),
                    FechaPago = reader.GetDateTime("FechaPago"),
                    Estado = reader.GetString("Estado"),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? "" : reader.GetString("Observaciones")
                };
            }
            return null;
        }
    }
}