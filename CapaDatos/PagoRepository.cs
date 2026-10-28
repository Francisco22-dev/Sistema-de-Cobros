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
                           "p.InscripcionId, p.ConceptoId, p.FechaPago, p.FechaVencimiento, p.Total, p.TotalEnVes, p.TotalEnUSD, " +
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

            if (!string.IsNullOrWhiteSpace(filtro))
                cmd.Parameters.AddWithValue("@filtro", "%" + filtro + "%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var pago = MapearPago(reader);
                pago.NombreEstudiante = reader.GetString("NombreEstudiante");
                pago.CursoHorario = reader.IsDBNull(reader.GetOrdinal("CursoHorario")) ? "" : reader.GetString("CursoHorario");
                pago.TotalEnUSD = reader.GetDecimal("TotalEnUSD");
                lista.Add(pago);
            }
            return lista;
        }

        public bool TieneDeuda(string cedula, DateTime hoy)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM Pagos WHERE EstudianteCedula = @cedula " +
                "AND (Estado = 'Vencido' OR (Estado = 'Vigente' AND FechaVencimiento < @hoy))", conn);
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
                using var cmdPago = new MySqlCommand(
                    "INSERT INTO Pagos (NumeroRecibo, EstudianteCedula, InscripcionId, ConceptoId, " +
                    "FechaPago, FechaVencimiento, Total, TotalEnVes, TotalEnUSD, MonedaCodigo, UsuarioRegistra, Estado, Observaciones) " +
                    "VALUES (@recibo, @cedula, @inscripcion, @concepto, @fechaPago, @vencimiento, @total, @totalVes, @totalUsd, @moneda, @usuario, @estado, @obs)", conn, transaction);
                cmdPago.Parameters.AddWithValue("@recibo", pago.NumeroRecibo);
                cmdPago.Parameters.AddWithValue("@cedula", pago.EstudianteCedula);
                cmdPago.Parameters.AddWithValue("@inscripcion", pago.InscripcionId);
                cmdPago.Parameters.AddWithValue("@concepto", pago.ConceptoId);
                cmdPago.Parameters.AddWithValue("@fechaPago", pago.FechaPago);
                cmdPago.Parameters.AddWithValue("@vencimiento", pago.FechaVencimiento);
                cmdPago.Parameters.AddWithValue("@total", pago.Total);
                cmdPago.Parameters.AddWithValue("@totalVes", pago.TotalEnVes);
                cmdPago.Parameters.AddWithValue("@totalUsd", pago.TotalEnUSD);
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
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public decimal ObtenerTotalHoy(DateTime fechaApertura)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "SELECT COALESCE(SUM(TotalEnVes), 0) FROM Pagos " +
                "WHERE FechaPago >= @fechaApertura AND Estado = 'Vigente'", conn);
            cmd.Parameters.AddWithValue("@fechaApertura", fechaApertura);
            var result = cmd.ExecuteScalar();
            return result == DBNull.Value ? 0 : Convert.ToDecimal(result