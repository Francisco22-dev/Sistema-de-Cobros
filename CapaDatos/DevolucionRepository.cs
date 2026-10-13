using System;
using MySqlConnector;

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
    }
}