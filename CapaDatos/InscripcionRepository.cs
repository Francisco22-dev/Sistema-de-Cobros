using System;
using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    public class InscripcionRepository
    {
        public List<InscripcionInfo> ObtenerInscripcionesActivas()
        {
            var lista = new List<InscripcionInfo>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "SELECT i.Id, i.GrupoId, g.CursoCodigo, g.Codigo AS GrupoCodigo, e.Cedula, e.NombreCompleto, e.Telefono, c.Nombre AS Curso, COALESCE(g.Nombre, 'Sin Horario') AS Horario, i.Estado, i.Observaciones " +
                "FROM Inscripciones i " +
                "JOIN Estudiantes e ON i.EstudianteCedula = e.Cedula " +
                "JOIN Grupos g ON i.GrupoId = g.Id " +
                "JOIN Cursos c ON g.CursoCodigo = c.Codigo " +
                "WHERE i.Estado = 'Activa' " +
                "ORDER BY e.NombreCompleto", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new InscripcionInfo
                {
                    InscripcionId = reader.GetInt32("Id"),
                    GrupoId = reader.GetInt32("GrupoId"),
                    CursoCodigo = reader.GetString("CursoCodigo"),
                    GrupoCodigo = reader.GetString("GrupoCodigo"),
                    Cedula = reader.GetString("Cedula"),
                    NombreCompleto = reader.GetString("NombreCompleto"),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? "" : reader.GetString("Telefono"),
                    CursoNombre = reader.GetString("Curso"),
                    GrupoNombre = reader.GetString("Horario"),
                    Estado = reader.GetString("Estado"),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? "" : reader.GetString("Observaciones")
                });
            }
            return lista;
        }

        public List<InscripcionInfo> ObtenerTodasLasInscripciones(string filtro = "", string filtroEstado = "")
        {
            var lista = new List<InscripcionInfo>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();

            string query =
                "SELECT i.Id, i.GrupoId, g.CursoCodigo, g.Codigo AS GrupoCodigo, e.Cedula, e.NombreCompleto, e.Telefono, c.Nombre AS Curso, COALESCE(g.Nombre, 'Sin Horario') AS Horario, i.Estado, i.Observaciones " +
                "FROM Inscripciones i " +
                "JOIN Estudiantes e ON i.EstudianteCedula = e.Cedula " +
                "JOIN Grupos g ON i.GrupoId = g.Id " +
                "JOIN Cursos c ON g.CursoCodigo = c.Codigo " +
                "WHERE 1=1 ";

            if (!string.IsNullOrWhiteSpace(filtro))
                query += "AND (e.Cedula LIKE @filtro OR e.NombreCompleto LIKE @filtro OR c.Nombre LIKE @filtro OR g.Codigo LIKE @filtro) ";

            if (!string.IsNullOrWhiteSpace(filtroEstado) && filtroEstado != "Todos")
                query += "AND i.Estado = @estado ";

            query += "ORDER BY i.Id DESC LIMIT 250";

            using var cmd = new MySqlCommand(query, conn);
            if (!string.IsNullOrWhiteSpace(filtro))
                cmd.Parameters.AddWithValue("@filtro", $"%{filtro}%");
            if (!string.IsNullOrWhiteSpace(filtroEstado) && filtroEstado != "Todos")
                cmd.Parameters.AddWithValue("@estado", filtroEstado);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new InscripcionInfo
                {
                    InscripcionId = reader.GetInt32("Id"),
                    GrupoId = reader.GetInt32("GrupoId"),
                    CursoCodigo = reader.GetString("CursoCodigo"),
                    GrupoCodigo = reader.GetString("GrupoCodigo"),
                    Cedula = reader.IsDBNull(reader.GetOrdinal("Cedula")) ? "" : reader.GetString("Cedula"),
                    NombreCompleto = reader.IsDBNull(reader.GetOrdinal("NombreCompleto")) ? "" : reader.GetString("NombreCompleto"),
                    Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? "" : reader.GetString("Telefono"),
                    CursoNombre = reader.IsDBNull(reader.GetOrdinal("Curso")) ? "" : reader.GetString("Curso"),
                    GrupoNombre = reader.IsDBNull(reader.GetOrdinal("Horario")) ? "Sin Horario" : reader.GetString("Horario"),
                    Estado = reader.IsDBNull(reader.GetOrdinal("Estado")) ? "Activa" : reader.GetString("Estado"),
                    Observaciones = reader.IsDBNull(reader.GetOrdinal("Observaciones")) ? "" : reader.GetString("Observaciones")
                });
            }
            return lista;
        }

        public void CambiarGrupo(int inscripcionId, int nuevoGrupoId, string nuevaObservacion)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "UPDATE Inscripciones SET GrupoId = @nuevoGrupoId, Observaciones = @observaciones WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@nuevoGrupoId", nuevoGrupoId);
            cmd.Parameters.AddWithValue("@observaciones", nuevaObservacion);
            cmd.Parameters.AddWithValue("@id", inscripcionId);
            cmd.ExecuteNonQuery();
        }

        public void Agregar(Inscripcion inscripcion)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "INSERT INTO Inscripciones (EstudianteCedula, GrupoId, Estado, Observaciones) " +
                "VALUES (@cedula, @grupoId, @estado, @obs)", conn);
            cmd.Parameters.AddWithValue("@cedula", inscripcion.EstudianteCedula);
            cmd.Parameters.AddWithValue("@grupoId", inscripcion.GrupoId);
            cmd.Parameters.AddWithValue("@estado", inscripcion.Estado);
            cmd.Parameters.AddWithValue("@obs", inscripcion.Observaciones ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        public void ActualizarEstado(int inscripcionId, string nuevoEstado, string? motivo)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "UPDATE Inscripciones SET Estado = @estado, " +
                "Observaciones = IF(@motivo IS NOT NULL AND @motivo != '', @motivo, Observaciones) " +
                "WHERE Id = @id", conn);
            cmd.Parameters.AddWithValue("@estado", nuevoEstado);
            cmd.Parameters.AddWithValue("@motivo", string.IsNullOrWhiteSpace(motivo) ? (object)DBNull.Value : motivo.Trim());
            cmd.Parameters.AddWithValue("@id", inscripcionId);
            cmd.ExecuteNonQuery();
        }
        

        public void EjecutarCambioDeCurso(int inscripcionViejaId, int nuevoGrupoId, string cedula,
            bool transferirTodosPagos, bool transferirInscripcion, string notaOrigen, string notaDestino)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Cerrar la inscripción anterior
                using var cmdCerrar = new MySqlCommand(
                    "UPDATE Inscripciones SET Estado = 'Cancelada', Observaciones = @obs WHERE Id = @id", conn, trans);
                cmdCerrar.Parameters.AddWithValue("@obs", notaOrigen);
                cmdCerrar.Parameters.AddWithValue("@id", inscripcionViejaId);
                cmdCerrar.ExecuteNonQuery();

                // 2. Crear la nueva inscripción en el nuevo grupo
                using var cmdNueva = new MySqlCommand(
                    "INSERT INTO Inscripciones (EstudianteCedula, GrupoId, Estado, Observaciones) " +
                    "VALUES (@cedula, @grupoId, 'Activa', @obs); SELECT LAST_INSERT_ID();", conn, trans);
                cmdNueva.Parameters.AddWithValue("@cedula", cedula);
                cmdNueva.Parameters.AddWithValue("@grupoId", nuevoGrupoId);
                cmdNueva.Parameters.AddWithValue("@obs", notaDestino);
                int nuevaInscripcionId = Convert.ToInt32(cmdNueva.ExecuteScalar());

                // 3. Trasladar recibos como crédito (re-vinculación atómica de pagos)
                if (transferirTodosPagos)
                {
                    using var cmdMoverPagos = new MySqlCommand(
                        "UPDATE Pagos SET InscripcionId = @nuevaId WHERE InscripcionId = @viejaId AND ConceptoId IN (2, 3)", conn, trans);
                    cmdMoverPagos.Parameters.AddWithValue("@nuevaId", nuevaInscripcionId);
                    cmdMoverPagos.Parameters.AddWithValue("@viejaId", inscripcionViejaId);
                    cmdMoverPagos.ExecuteNonQuery();
                }

                // 4. Trasladar matrícula si fue convalidada
                if (transferirInscripcion)
                {
                    using var cmdMoverIns = new MySqlCommand(
                        "UPDATE Pagos SET InscripcionId = @nuevaId WHERE InscripcionId = @viejaId AND ConceptoId = 1", conn, trans);
                    cmdMoverIns.Parameters.AddWithValue("@nuevaId", nuevaInscripcionId);
                    cmdMoverIns.Parameters.AddWithValue("@viejaId", inscripcionViejaId);
                    cmdMoverIns.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
        // ========================================================
        // VALIDACIÓN MULTICURSO Y NO DUPLICIDAD ACTIVA
        // ========================================================
        public (bool Existe, bool EstaActiva, string EstadoPrevio, string GrupoCodigo, string CursoNombre)
            VerificarHistorialEstudianteEnCurso(string cedula, string cursoCodigo)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "SELECT i.Id, i.Estado, g.Codigo AS GrupoCodigo, c.Nombre AS CursoNombre " +
                "FROM Inscripciones i " +
                "JOIN Grupos g ON i.GrupoId = g.Id " +
                "JOIN Cursos c ON g.CursoCodigo = c.Codigo " +
                "WHERE i.EstudianteCedula = @cedula AND c.Codigo = @cursoCod " +
                "ORDER BY i.Id DESC LIMIT 1", conn);

            cmd.Parameters.AddWithValue("@cedula", cedula.Trim());
            cmd.Parameters.AddWithValue("@cursoCod", cursoCodigo.Trim());

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                string estado = reader.GetString("Estado");
                string grupo = reader.GetString("GrupoCodigo");
                string curso = reader.GetString("CursoNombre");
                return (true, estado == "Activa", estado, grupo, curso);
            }

            return (false, false, "", "", "");
        }

        // ========================================================
        // TRASLADO DE CARRERA RECONOCIENDO CLASES (1 A 1)
        // ========================================================
        public void EjecutarCambioCursoPorClases(
            int inscripcionViejaId,
            int nuevoGrupoId,
            string cedula,
            decimal montoCreditoClasesUSD,
            decimal montoMatriculaConvalidadaUSD,
            string notaOrigen,
            string notaDestino,
            string cursoOrigenNombre)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var trans = conn.BeginTransaction();

            try
            {
                // 1. Cancelar la inscripción anterior
                using var cmdCerrar = new MySqlCommand(
                    "UPDATE Inscripciones SET Estado = 'Cancelada', Observaciones = @obs WHERE Id = @id", conn, trans);
                cmdCerrar.Parameters.AddWithValue("@obs", notaOrigen);
                cmdCerrar.Parameters.AddWithValue("@id", inscripcionViejaId);
                cmdCerrar.ExecuteNonQuery();

                // 2. Crear la nueva inscripción en el nuevo grupo
                using var cmdNueva = new MySqlCommand(
                    "INSERT INTO Inscripciones (EstudianteCedula, GrupoId, Estado, Observaciones) " +
                    "VALUES (@cedula, @grupoId, 'Activa', @obs); SELECT LAST_INSERT_ID();", conn, trans);
                cmdNueva.Parameters.AddWithValue("@cedula", cedula);
                cmdNueva.Parameters.AddWithValue("@grupoId", nuevoGrupoId);
                cmdNueva.Parameters.AddWithValue("@obs", notaDestino);
                int nuevaInscripcionId = Convert.ToInt32(cmdNueva.ExecuteScalar());

                // 3. Crear el abono de crédito por las clases reconocidas en el nuevo curso
                if (montoCreditoClasesUSD > 0)
                {
                    string reciboCredito = $"REC-TRASLADO-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

                    using var cmdPagoClases = new MySqlCommand(
                        "INSERT INTO Pagos (NumeroRecibo, EstudianteCedula, InscripcionId, ConceptoId, FechaPago, FechaVencimiento, Total, TotalEnVes, MonedaCodigo, UsuarioRegistra, Estado, Observaciones) " +
                        "VALUES (@recibo, @cedula, @insId, 3, NOW(), CURDATE(), @total, @total, 'BCV', 'admin', 'Vigente', @obs); SELECT LAST_INSERT_ID();", conn, trans);
                    cmdPagoClases.Parameters.AddWithValue("@recibo", reciboCredito);
                    cmdPagoClases.Parameters.AddWithValue("@cedula", cedula);
                    cmdPagoClases.Parameters.AddWithValue("@insId", nuevaInscripcionId);
                    cmdPagoClases.Parameters.AddWithValue("@total", montoCreditoClasesUSD);
                    cmdPagoClases.Parameters.AddWithValue("@obs", $"Crédito de clases reconocidas por traslado desde {cursoOrigenNombre}");
                    int pagoClasesId = Convert.ToInt32(cmdPagoClases.ExecuteScalar());

                    using var cmdDetalleClases = new MySqlCommand(
                        "INSERT INTO PagoDetalles (PagoId, MetodoPagoId, Monto, MonedaCodigo, TasaCambio, MontoEnVes, Referencia) " +
                        "VALUES (@pagoId, 1, @monto, 'BCV', 1.000000, @monto, 'TRASLADO-CLASES')", conn, trans);
                    cmdDetalleClases.Parameters.AddWithValue("@pagoId", pagoClasesId);
                    cmdDetalleClases.Parameters.AddWithValue("@monto", montoCreditoClasesUSD);
                    cmdDetalleClases.ExecuteNonQuery();
                }

                // 4. Convalidar Matrícula (Exoneración oficial de inscripción)
                if (montoMatriculaConvalidadaUSD > 0)
                {
                    string reciboMatricula = $"REC-MAT-TRASLADO-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

                    using var cmdPagoMat = new MySqlCommand(
                        "INSERT INTO Pagos (NumeroRecibo, EstudianteCedula, InscripcionId, ConceptoId, FechaPago, FechaVencimiento, Total, TotalEnVes, MonedaCodigo, UsuarioRegistra, Estado, Observaciones) " +
                        "VALUES (@recibo, @cedula, @insId, 1, NOW(), CURDATE(), @total, @total, 'BCV', 'admin', 'Vigente', @obs); SELECT LAST_INSERT_ID();", conn, trans);
                    cmdPagoMat.Parameters.AddWithValue("@recibo", reciboMatricula);
                    cmdPagoMat.Parameters.AddWithValue("@cedula", cedula);
                    cmdPagoMat.Parameters.AddWithValue("@insId", nuevaInscripcionId);
                    cmdPagoMat.Parameters.AddWithValue("@total", montoMatriculaConvalidadaUSD);
                    cmdPagoMat.Parameters.AddWithValue("@obs", $"Matrícula convalidada y exonerada por traslado desde {cursoOrigenNombre}");
                    int pagoMatId = Convert.ToInt32(cmdPagoMat.ExecuteScalar());

                    using var cmdDetalleMat = new MySqlCommand(
                        "INSERT INTO PagoDetalles (PagoId, MetodoPagoId, Monto, MonedaCodigo, TasaCambio, MontoEnVes, Referencia) " +
                        "VALUES (@pagoId, 1, @monto, 'BCV', 1.000000, @monto, 'CONVALIDADA')", conn, trans);
                    cmdDetalleMat.Parameters.AddWithValue("@pagoId", pagoMatId);
                    cmdDetalleMat.Parameters.AddWithValue("@monto", montoMatriculaConvalidadaUSD);
                    cmdDetalleMat.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }
    }
}