using System;
using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    public class MovimientoCajaRepository
    {
        public void Registrar(MovimientoCaja mov)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand(
                "INSERT INTO MovimientosCaja (CajaId, Tipo, Monto, MonedaCodigo, Metodo, Concepto, Categoria, FechaMovimiento, UsuarioRegistra) " +
                "VALUES (@cajaId, @tipo, @monto, @moneda, @metodo, @concepto, @categoria, @fecha, @usuario)", conn);
            cmd.Parameters.AddWithValue("@cajaId", mov.CajaId);
            cmd.Parameters.AddWithValue("@tipo", mov.Tipo);
            cmd.Parameters.AddWithValue("@monto", mov.Monto);
            cmd.Parameters.AddWithValue("@moneda", mov.MonedaCodigo);
            cmd.Parameters.AddWithValue("@metodo", mov.Metodo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@concepto", mov.Concepto);
            cmd.Parameters.AddWithValue("@categoria", mov.Categoria ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@fecha", mov.FechaMovimiento);
            cmd.Parameters.AddWithValue("@usuario", mov.UsuarioRegistra);
            cmd.ExecuteNonQuery();
        }

        public List<MovimientoCaja> ObtenerMovimientosPorTipo(string tipo)
        {
            var lista = new List<MovimientoCaja>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT * FROM MovimientosCaja WHERE Tipo = @tipo ORDER BY FechaMovimiento DESC", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new MovimientoCaja(
                    reader.GetInt32("CajaId"), reader.GetString("Tipo"), reader.GetDecimal("Monto"),
                    reader.GetString("MonedaCodigo"), reader.GetString("Concepto"), reader.GetString("UsuarioRegistra"))
                {
                    Id = reader.GetInt32("Id"),
                    Metodo = reader.IsDBNull(reader.GetOrdinal("Metodo")) ? "Otro" : reader.GetString("Metodo"),
                    Categoria = reader.IsDBNull(reader.GetOrdinal("Categoria")) ? null : reader.GetString("Categoria"),
                    FechaMovimiento = reader.GetDateTime("FechaMovimiento")
                });
            }
            return lista;
        }

        public List<ArqueoItem> ObtenerDesgloseMovimientosGlobal()
        {
            var lista = new List<ArqueoItem>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            // Suma Ingresos manuales y Resta Egresos para saber cuánto hay físicamente por método
            using var cmd = new MySqlCommand("SELECT COALESCE(Metodo, 'Otro') AS Metodo, MonedaCodigo AS Moneda, SUM(IF(Tipo='Ingreso', Monto, -Monto)) AS Total FROM MovimientosCaja GROUP BY Metodo, MonedaCodigo", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new ArqueoItem { Metodo = reader.GetString("Metodo"), Moneda = reader.GetString("Moneda"), Total = reader.GetDecimal("Total") });
            }
            return lista;
        }

        public List<string> ObtenerCategoriasHistoricas()
        {
            var lista = new List<string>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT DISTINCT Categoria FROM MovimientosCaja WHERE Categoria IS NOT NULL AND Tipo = 'Egreso'", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) lista.Add(reader.GetString(0));
            return lista;
        }
    }
}