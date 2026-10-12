using System;
using System.Collections.Generic;
using MySqlConnector;
using Entidades;

namespace CapaDatos
{
    /// <summary>
    /// Repositorio de auditoría en MariaDB con IP y anti-SQL injection.
    /// </summary>
    public class AuditoriaRepository
    {
        public void Registrar(Auditoria auditoria)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            conn.Open();
            using var cmd = new MySqlCommand(
                "INSERT INTO Auditoria (Usuario, Accion, TablaAfectada, RegistroId, DatosAnteriores, DatosNuevos, Computadora, Ip, FechaHora, Exito) " +
                "VALUES (@usuario, @accion, @tabla, @registroId, @antes, @despues, @pc, @ip, @fecha, @exito)", conn);
            cmd.Parameters.AddWithValue("@usuario", auditoria.Usuario);
            cmd.Parameters.AddWithValue("@accion", auditoria.Accion);
            cmd.Parameters.AddWithValue("@tabla", auditoria.TablaAfectada);
            cmd.Parameters.AddWithValue("@registroId", auditoria.RegistroId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@antes", auditoria.DatosAnteriores ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@despues", auditoria.DatosNuevos ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@pc", auditoria.Computadora);
            cmd.Parameters.AddWithValue("@ip", auditoria.Ip ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@fecha", auditoria.FechaHora);
            cmd.Parameters.AddWithValue("@exito", auditoria.Exito);
            cmd.ExecuteNonQuery();
        }

        public List<Auditoria> ObtenerTodos()
        {
            var lista = new List<Auditoria>();
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT * FROM Auditoria ORDER BY FechaHora DESC", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(MapearAuditoria(reader));
            }
            return lista;
        }

        private Auditoria MapearAuditoria(MySqlDataReader reader)
        {
            return new Auditoria(
                reader.GetString("Usuario"),
                reader.GetString("Accion"),
                reader.GetString("TablaAfectada"),
                reader.IsDBNull(reader.GetOrdinal("DatosAnteriores")) ? null! : reader.GetString("DatosAnteriores"),
                reader.IsDBNull(reader.GetOrdinal("DatosNuevos")) ? null! : reader.GetString("DatosNuevos"),
                reader.GetString("Computadora"),
                reader.IsDBNull(reader.GetOrdinal("Ip")) ? null : reader.GetString("Ip"),
                reader.GetBoolean("Exito"))
            {
                Id = reader.GetInt32("Id"),
                RegistroId = reader.IsDBNull(reader.GetOrdinal("RegistroId")) ? null : reader.GetString("RegistroId"),
                FechaHora = reader.GetDateTime("FechaHora")
            };
        }
    }
}
