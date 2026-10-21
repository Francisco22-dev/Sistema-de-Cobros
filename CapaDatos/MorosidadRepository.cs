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

            // 1. Buscamos las tasas de HOY (Viernes, Martes, el día que sea)
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

            // 2. FÓRMULA DE DEUDA ESTRICTA
            // Ahora SOLO descuenta dinero si el estudiante pagó Mensualidad (2) o Semanal (3).
            // Si paga un certificado o inscripción, la deuda de las clases no se toca.
            string query = @"
                SELECT 
                    e.Cedula, e.NombreCompleto, e.Telefono,
                    c.Nombre AS Curso,
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
                decimal precioClase = reader.GetDecimal("PrecioClase"); // Ej. 12$
                decimal pagadoUSD = reader.GetDecimal("TotalPagadoHistoricoUSD");

                // 3. LA MATEMÁTICA LÓGICA QUE EXPLICASTE
                decimal costoTotalUSD = clases * precioClase; // Ej. 2 clases x 12$ = 24$
                decimal deudaUSD = Math.Round(costoTotalUSD - pagadoUSD, 2); // Redondeamos a 2 decimales para matar el bug de los céntimos

                if (deudaUSD > 0) // Si la deuda es mayor a cero, está moroso
                {
                    // Multiplicamos esos dólares fijos por la tasa que haya amanecido HOY
                    decimal deudaBCV = Math.Round(deudaUSD * tasaBCV, 2);
                    decimal deudaEUR = tasaEUR > 0 ? Math.Round(deudaBCV / tasaEUR, 2) : 0;
                    decimal deudaUSDT = tasaUSDT > 0 ? Math.Round(deudaBCV / tasaUSDT, 2) : 0;

                    lista.Add(new MorosidadInfo
                    {
                        Cedula = reader.GetString("Cedula"),
                        NombreCompleto = reader.GetString("NombreCompleto"),
                        Telefono = reader.GetString("Telefono"),
                        Curso = reader.GetString("Curso"),
                        ClasesImpartidas = clases,
                        TotalPagado = pagadoUSD,
                        DeudaUSD = deudaUSD,
                        DeudaBCV = deudaBCV,
                        DeudaEUR = deudaEUR,
                        DeudaUSDT = deudaUSDT
                    });
                }
            }
            return lista;
        }
    }
}