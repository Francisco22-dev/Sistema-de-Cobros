using System;
using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    public class PagoRepository
    {
        public List<Pago> ObtenerRecientes(int limite = 100, string filtro = "")
        {
            var lista = new List<Pago>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            string query = "SELECT p.Id, p.NumeroRecibo, p.EstudianteCedula, e.NombreCompleto AS NombreEstudiante, " +
                           "p.InscripcionId, p.ConceptoId, p.FechaPago, p.FechaVencimiento, p.Total, p.TotalEnVes, " +
                           "p.MonedaCodigo, p.UsuarioRegistra, p.Estado, p.Observaciones, " +
                           "CONCAT(c.Nombre, ' (', COALESCE(g.Nombre, 'Sin Horario'), ')') AS CursoHorario " +
                           "FROM Pagos p " +
                           "JOIN Estudiantes e ON p.EstudianteCedula = e.Cedula " +
                           "LEFT JOIN Inscripciones i ON p.InscripcionId = i.Id " +
                           "LEFT JOIN Grupos g ON i.GrupoId = g.Id " +
                           "LEFT JOIN Cursos c ON g.CursoCodigo = c.Codigo ";

            if (!string.IsNullOrWhiteSpace(filtro))
                query += "WHERE p.EstudianteCedula LIKE @filtro OR e.NombreCompleto LIKE @filtro ";
            query += "ORDER BY p.FechaPago DESC LIMIT @limite";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@limite", limite);
            if (!string.IsNullOrWhiteSpace(filtro)) cmd.Parameters.AddWithValue("@filtro", "%" + filtro + "%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var pago = MapearPago(reader);
                pago.NombreEstudiante = reader.GetString("NombreEstudiante");
                pago.CursoHorario = reader.IsDBNull(reader.GetOrdinal("CursoHorario")) ? "" : reader.GetString("CursoHorario");
                lista.Add(pago);
            }
            return lista;
        }

        public bool TieneDeuda(string cedula, DateTime hoy)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT COUNT(*) FROM Pagos WHERE EstudianteCedula = @cedula AND (Estado = 'Vencido' OR (Estado = 'Vigente' AND FechaVencimiento < @hoy))", conn);
            cmd.Parameters.AddWithValue("@cedula", cedula);
            cmd.Parameters.AddWithValue("@hoy", hoy);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public int Agregar(Pago pago, List<PagoDetalle> detalles)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var transaction = conn.BeginTransaction();
            try
            {
                // Limpio sin el TotalEnUSD
                using var cmdPago = new MySqlCommand(
                    "INSERT INTO Pagos (NumeroRecibo, EstudianteCedula, InscripcionId, ConceptoId, FechaPago, FechaVencimiento, Total, TotalEnVes, MonedaCodigo, UsuarioRegistra, Estado, Observaciones) " +
                    "VALUES (@recibo, @cedula, @inscripcion, @concepto, @fechaPago, @vencimiento, @total, @totalVes, @moneda, @usuario, @estado, @obs)", conn, transaction);
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
                        "VALUES (@pagoId, @metodo, @monto, @moneda, @tasa, @montoVes, @referencia)", conn, transaction);
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
            catch { transaction.Rollback(); throw; }
        }

        public decimal ObtenerTotalHoy(DateTime fechaApertura)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT COALESCE(SUM(TotalEnVes), 0) FROM Pagos WHERE FechaPago >= @fechaApertura AND Estado = 'Vigente'", conn);
            cmd.Parameters.AddWithValue("@fechaApertura", fechaApertura);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
        }

        public List<PagoDetalle> ObtenerDetalles(int pagoId)
        {
            var lista = new List<PagoDetalle>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT pd.*, mp.Nombre AS MetodoNombre FROM PagoDetalles pd JOIN MetodosPago mp ON pd.MetodoPagoId = mp.Id WHERE pd.PagoId = @pagoId", conn);
            cmd.Parameters.AddWithValue("@pagoId", pagoId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new PagoDetalle(reader.GetInt32("PagoId"), reader.GetInt32("MetodoPagoId"), reader.GetDecimal("Monto"), reader.GetString("MonedaCodigo"), reader.GetDecimal("TasaCambio"), reader.GetDecimal("MontoEnVes"), reader.IsDBNull(reader.GetOrdinal("Referencia")) ? null : reader.GetString("Referencia")) { Id = reader.GetInt32("Id"), MetodoNombre = reader.GetString("MetodoNombre") });
            }
            return lista;
        }

        private Pago MapearPago(MySqlDataReader reader)
        {
            return new Pago(reader.GetString("NumeroRecibo"), reader.GetString("EstudianteCedula"), reader.GetInt32("InscripcionId"), reader.GetInt32("ConceptoId"), reader.GetDecimal("Total"), reader.GetString("MonedaCodigo"), reader.GetString("UsuarioRegistra"), reader.GetDateTime("FechaVencimiento"))
            { Id = reader.GetInt32("Id"), FechaPago = reader.GetDateTime("FechaPago"), TotalEnVes = reader.GetDecimal("TotalEnVes"), Estado = reader.GetString("Estado"), Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? null : reader.GetString("Observaciones") };
        }
        
        // Extrae la suma de TODOS los ingresos históricos protegidos en Dólares mediante cálculo matemático
        public decimal ObtenerIngresosTotalesUSD()
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();

            // Hacemos el cálculo al vuelo: Bolívares / Tasa de ese día
            string query = @"
                SELECT COALESCE(SUM(p.TotalEnVes / COALESCE((SELECT tc.Tasa FROM TasasCambio tc WHERE tc.Fecha = DATE(p.FechaPago) AND tc.MonedaOrigen = 'BCV' LIMIT 1), 1)), 0) 
                FROM Pagos p 
                WHERE p.Estado = 'Vigente'";

            using var cmd = new MySqlCommand(query, conn);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0 : Convert.ToDecimal(result);
        }
    }
}