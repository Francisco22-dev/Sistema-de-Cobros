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
                "INSERT INTO MovimientosCaja (CajaId, Tipo, Monto, MonedaCodigo, Concepto, Categoria, FechaMovimiento, UsuarioRegistra) " +
                "VALUES (@cajaId, @tipo, @monto, @moneda, @concepto, @categoria, @fecha, @usuario)", conn);
            cmd.Parameters.AddWithValue("@cajaId", mov.CajaId);
            cmd.Parameters.AddWithValue("@tipo", mov.Tipo);
            cmd.Parameters.AddWithValue("@monto", mov.Monto);
            cmd.Parameters.AddWithValue("@moneda", mov.MonedaCodigo);
            cmd.Parameters.AddWithValue("@concepto", mov.Concepto);
            cmd.Parameters.AddWithValue("@categoria", mov.Categoria ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@fecha", mov.FechaMovimiento);
            cmd.Parameters.AddWithValue("@usuario", mov.UsuarioRegistra);
            cmd.ExecuteNonQuery();
        }

        public List<MovimientoCaja> ObtenerPorCajaYTipo(int cajaId, string tipo)
        {
            var lista = new List<MovimientoCaja>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT * FROM MovimientosCaja WHERE CajaId = @cajaId AND Tipo = @tipo ORDER BY FechaMovimiento DESC", conn);
            cmd.Parameters.AddWithValue("@cajaId", cajaId);
            cmd.Parameters.AddWithValue("@tipo", tipo);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new MovimientoCaja(
                    reader.GetInt32("CajaId"), reader.GetString("Tipo"), reader.GetDecimal("Monto"),
                    reader.GetString("MonedaCodigo"), reader.GetString("Concepto"), reader.GetString("UsuarioRegistra"))
                {
                    Id = reader.GetInt32("Id"),
                    Categoria = reader.IsDBNull(reader.GetOrdinal("Categoria")) ? null : reader.GetString("Categoria"),
                    FechaMovimiento = reader.GetDateTime("FechaMovimiento")
                });
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

        public List<MovimientoCaja> ObtenerTodosLosEgresos()
        {
            var lista = new List<MovimientoCaja>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("SELECT * FROM MovimientosCaja WHERE Tipo = 'Egreso' ORDER BY FechaMovimiento DESC", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new MovimientoCaja(
                    reader.GetInt32("CajaId"), reader.GetString("Tipo"), reader.GetDecimal("Monto"),
                    reader.GetString("MonedaCodigo"), reader.GetString("Concepto"), reader.GetString("UsuarioRegistra"))
                {
                    Id = reader.GetInt32("Id"),
                    Categoria = reader.IsDBNull(reader.GetOrdinal("Categoria")) ? null : reader.GetString("Categoria"),
                    FechaMovimiento = reader.GetDateTime("FechaMovimiento")
                });
            }
            return lista;
        }
    }
}