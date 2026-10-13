using System;
using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    public class PagoRepository
    {
        public List<Pago> ObtenerRecientes(int limite = 100)
        {
            var lista = new List<Pago>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "SELECT Id, NumeroRecibo, EstudianteCedula, InscripcionId, ConceptoId, " +
                "FechaPago, FechaVencimiento, Total, TotalEnVes, MonedaCodigo, UsuarioRegistra, Estado, Observaciones " +
                "FROM Pagos ORDER BY FechaPago DESC LIMIT @limite", conn);
            cmd.Parameters.AddWithValue("@limite", limite);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapearPago(reader));
            }
            return lista;
        }

        // NUEVO: Revisa si el estudiante tiene al menos 1 pago vencido
        public bool TieneDeuda(string cedula)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM Pagos WHERE EstudianteCedula = @cedula " +
                "AND (Estado = 'Vencido' OR (Estado = 'Vigente' AND FechaVencimiento < CURDATE()))", conn);
            cmd.Parameters.AddWithValue("@cedula", cedula);

            int pagosVencidos = Convert.ToInt32(cmd.ExecuteScalar());
            return pagosVencidos > 0;
        }

        public int Agregar(Pago pago, List<PagoDetalle> detalles)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var transaction = conn.BeginTransaction();

            try
            {
                using var cmdPago = new MySqlCommand(
                    "INSERT INTO Pagos (NumeroRecibo, EstudianteCedula, InscripcionId, ConceptoId, " +
                    "FechaPago, FechaVencimiento, Total, TotalEnVes, MonedaCodigo, UsuarioRegistra, Estado, Observaciones) " +
                    "VALUES (@recibo, @cedula, @inscripcion, @concepto, @fechaPago, @vencimiento, @total, @totalVes, @moneda, @usuario, @estado, @obs)", conn);
                cmdPago.Transaction = transaction;
                cmdPago.Parameters.AddWithValue("@recibo", pago.NumeroRecibo);
                cmdPago.Parameters.AddWithValue("@cedula", pago.EstudianteCedula);
                cmdPago.Parameters.AddWithValue("@inscripcion", pago.InscripcionId);
                cmdPago.Parameters.AddWithValue("@concepto", pago.ConceptoId);
                cmdPago.Parameters.AddWithValue("@fechaPago", pago.FechaPago);
                cmdPago.Parameters.AddWithValue("@vencimiento", pago.FechaVencimiento);
                cmdPago.Parameters.AddWithValue("@total", pago.Total);
                cmdPago.Parameters.AddWithValue("@totalVes", pago.TotalEnVes);
                cmdPago.Parameters.AddWithValue("@moneda", pago.MonedaCodigo);
                cmdPago.Parameters.AddWithValue("@usuario", pago.UsuarioRegistra);
                cmdPago.Parameters.AddWithValue("@estado", pago.Estado);
                cmdPago.Parameters.AddWithValue("@obs", pago.Observaciones ?? (object)DBNull.Value);
                cmdPago.ExecuteNonQuery();

                int pagoId = (int)cmdPago.LastInsertedId;

                foreach (var detalle in detalles)
                {
                    using var cmdDetalle = new MySqlCommand(
                        "INSERT INTO PagoDetalles (PagoId, MetodoPagoId, Monto, MonedaCodigo, TasaCambio, MontoEnVes, Referencia) " +
                        "VALUES (@pagoId, @metodo, @monto, @moneda, @tasa, @montoVes, @referencia)", conn);
                    cmdDetalle.Transaction = transaction;
                    cmdDetalle.Parameters.AddWithValue("@pagoId", pagoId);
                    cmdDetalle.Parameters.AddWithValue("@metodo", detalle.MetodoPagoId);
                    cmdDetalle.Parameters.AddWithValue("@monto", detalle.Monto);
                    cmdDetalle.Parameters.AddWithValue("@moneda", detalle.MonedaCodigo);
                    cmdDetalle.Parameters.AddWithValue("@tasa", detalle.TasaCambio);
                    cmdDetalle.Parameters.AddWithValue("@montoVes", detalle.MontoEnVes);
                    cmdDetalle.Parameters.AddWithValue("@referencia", detalle.Referencia ?? (object)DBNull.Value);
                    cmdDetalle.ExecuteNonQuery();
                }

                transaction.Commit();
                return pagoId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public decimal ObtenerTotalHoy()
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "SELECT COALESCE(SUM(TotalEnVes), 0) FROM Pagos " +
                "WHERE DATE(FechaPago) = CURDATE() AND Estado = 'Vigente'", conn);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
        }

        private Pago MapearPago(MySqlDataReader reader)
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
                FechaPago = reader.GetDateTime("FechaPago"),
                TotalEnVes = reader.GetDecimal("TotalEnVes"),
                Estado = reader.GetString("Estado"),
                Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? null : reader.GetString("Observaciones")
            };
        }
    }
}