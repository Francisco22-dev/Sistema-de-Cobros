using System;
using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    public class MorosidadRepository
    {
        public List<MorosidadInfo> ObtenerMorosos(DateTime hoy)
        {
            var lista = new List<MorosidadInfo>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();

            // 1. Tasas del día
            decimal tasaBCV = 1m, tasaEUR = 1m, tasaUSDT = 1m;
            using (var cmdTasas = new MySqlCommand("SELECT MonedaOrigen, Tasa FROM TasasCambio WHERE Fecha = @hoy", conn))
            {
                cmdTasas.Parameters.AddWithValue("@hoy", hoy);
                using var readerTasas = cmdTasas.ExecuteReader();
                while (readerTasas.Read())
                {
                    string moneda = readerTasas.GetString("MonedaOrigen");
                    decimal tasa = readerTasas.GetDecimal("Tasa");
                    if (moneda == "BCV") tasaBCV = tasa;
                    else if (moneda == "EUR") tasaEUR = tasa;
                    else if (moneda == "USDT") tasaUSDT = tasa;
                }
            }

            // 2. Consulta con Código de Grupo y concatenación limpia
            string query = @"
        SELECT 
            e.Cedula, e.NombreCompleto, e.Telefono,
            c.Codigo AS CursoCodigo,
            c.Nombre AS CursoNombre,
            g.Codigo AS GrupoCodigo,
            CONCAT(c.Nombre, ' (', g.Codigo, ')') AS CursoConGrupo,
            c.PrecioMensualidad AS PrecioClase,
            
            (SELECT COUNT(*) FROM CalendarioClases cc WHERE cc.GrupoId = i.GrupoId AND cc.FechaClase <= @hoy AND cc.Estado NOT IN ('Nula', 'Festivo')) AS ClasesImpartidas,
            
            COALESCE((
                SELECT SUM(p.TotalEnVes / COALESCE((SELECT tc.Tasa FROM TasasCambio tc WHERE tc.Fecha = DATE(p.FechaPago) AND tc.MonedaOrigen = 'BCV' LIMIT 1), 1)) 
                FROM Pagos p WHERE p.InscripcionId = i.Id AND p.Estado = 'Vigente' AND p.ConceptoId IN (2, 3)
            ), 0) AS TotalPagadoHistoricoUSD

        FROM Inscripciones i
        JOIN Estudiantes e ON i.EstudianteCedula = e.Cedula
        JOIN Grupos g ON i.GrupoId = g.Id
        JOIN Cursos c ON g.CursoCodigo = c.Codigo
        WHERE i.Estado = 'Activa'";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@hoy", hoy);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int clases = reader.GetInt32("ClasesImpartidas");
                decimal precioClase = reader.GetDecimal("PrecioClase");
                decimal pagadoUSD = reader.GetDecimal("TotalPagadoHistoricoUSD");

                decimal costoTotalUSD = clases * precioClase;
                decimal deudaUSD = Math.Round(costoTotalUSD - pagadoUSD, 2);

                if (deudaUSD > 0)
                {
                    decimal deudaBCV = Math.Round(deudaUSD * tasaBCV, 2);
                    decimal deudaEUR = Math.Round(deudaUSD * tasaEUR, 2);
                    decimal deudaUSDT = Math.Round(deudaUSD * tasaUSDT, 2);

                    string cursoConGrupo = reader.IsDBNull(reader.GetOrdinal("CursoConGrupo")) ? reader.GetString("CursoNombre") : reader.GetString("CursoConGrupo");
                    string grupoCodigo = reader.IsDBNull(reader.GetOrdinal("GrupoCodigo")) ? "" : reader.GetString("GrupoCodigo");
                    string cursoCodigo = reader.IsDBNull(reader.GetOrdinal("CursoCodigo")) ? "" : reader.GetString("CursoCodigo");

                    lista.Add(new MorosidadInfo
                    {
                        Cedula = reader.GetString("Cedula"),
                        NombreCompleto = reader.GetString("NombreCompleto"),
                        Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? "" : reader.GetString("Telefono"),
                        Curso = cursoConGrupo,
                        CursoCodigo = cursoCodigo,
                        GrupoCodigo = grupoCodigo,
                        ClasesImpartidas = clases,
                        TotalPagado = pagadoUSD,
                        DeudaUSD = deudaUSD,
                        DeudaBCV = deudaBCV,
                        DeudaEUR = deudaEUR,
                        DeudaUSDT = deudaUSDT,
                        PrecioClase = precioClase
                    });
                }
            }
            return lista;
        }
    }
}