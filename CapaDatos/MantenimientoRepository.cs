using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using MySqlConnector;

namespace CapaDatos
{
    public class MantenimientoRepository
    {
        public void OptimizarTablas()
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            using var cmd = new MySqlCommand("OPTIMIZE TABLE Pagos, PagoDetalles, Inscripciones, Estudiantes, Auditoria, MovimientosCaja", conn);
            cmd.CommandTimeout = 120;
            cmd.ExecuteNonQuery();
        }

        public void RespaldarBaseDeDatos(string rutaArchivo)
        {
            // 1. Intentar respaldar con mysqldump si está disponible
            string? rutaMysqldump = LocalizarMysqldump();

            if (!string.IsNullOrEmpty(rutaMysqldump))
            {
                try
                {
                    EjecutarDumpExterno(rutaMysqldump, rutaArchivo);
                    return;
                }
                catch
                {
                    // Si falla el proceso externo, pasa automáticamente al respaldo nativo
                }
            }

            // 2. Si no existe mysqldump en esta PC, se respalda directamente con C#
            GenerarRespaldoNativo(rutaArchivo);
        }

        private string? LocalizarMysqldump()
        {
            // Rutas comunes donde suele residir mysqldump
            var rutasPosibles = new List<string>
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mysqldump.exe"),
                @"C:\xampp\mysql\bin\mysqldump.exe",
                @"C:\Program Files\MariaDB 10.4\bin\mysqldump.exe",
                @"C:\Program Files\MariaDB 10.5\bin\mysqldump.exe",
                @"C:\Program Files\MariaDB 10.6\bin\mysqldump.exe",
                @"C:\Program Files\MariaDB 10.11\bin\mysqldump.exe",
                @"C:\Program Files\MySQL\MySQL Server 8.0\bin\mysqldump.exe",
                @"C:\wamp64\bin\mysql\mysql5.7.36\bin\mysqldump.exe"
            };

            foreach (var ruta in rutasPosibles)
            {
                if (File.Exists(ruta)) return ruta;
            }

            return null;
        }

        private void EjecutarDumpExterno(string ejecutableDump, string rutaArchivo)
        {
            string user = DatabaseConfig.UserId;
            string pass = DatabaseConfig.Password;
            string server = DatabaseConfig.Server;
            string port = DatabaseConfig.Port;
            string db = DatabaseConfig.Database;

            string argumentos = $"-h {server} -P {port} -u {user} --password=\"{pass}\" {db} --result-file=\"{rutaArchivo}\"";

            var processInfo = new ProcessStartInfo
            {
                FileName = ejecutableDump,
                Arguments = argumentos,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            process?.WaitForExit();

            if (process?.ExitCode != 0)
            {
                throw new Exception(process?.StandardError.ReadToEnd() ?? "Error en mysqldump.");
            }
        }

        /// <summary>
        /// Genera el archivo .sql directamente mediante la conexión de MariaDB.
        /// No requiere ningún ejecutable externo ni permisos de consola.
        /// </summary>
        private void GenerarRespaldoNativo(string rutaArchivo)
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            var sb = new StringBuilder();

            sb.AppendLine("-- ========================================================");
            sb.AppendLine($"-- RESPALDO DE BASE DE DATOS: {DatabaseConfig.Database}");
            sb.AppendLine($"-- FECHA Y HORA: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine("-- SISTEMA DE COBROS 2.0 - CEVENCA");
            sb.AppendLine("-- ========================================================\n");
            sb.AppendLine("SET FOREIGN_KEY_CHECKS = 0;");
            sb.AppendLine("SET SQL_MODE = \"NO_AUTO_VALUE_ON_ZERO\";\n");

            // 1. Obtener lista de tablas
            var tablas = new List<string>();
            using (var cmd = new MySqlCommand("SHOW TABLES", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read()) tablas.Add(reader.GetString(0));
            }

            // 2. Exportar estructura y datos de cada tabla
            foreach (var tabla in tablas)
            {
                // Estructura (CREATE TABLE)
                string createTableSql = "";
                using (var cmd = new MySqlCommand($"SHOW CREATE TABLE `{tabla}`", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read()) createTableSql = reader.GetString(1);
                }

                sb.AppendLine($"\n-- Estructura de la tabla `{tabla}`");
                sb.AppendLine($"DROP TABLE IF EXISTS `{tabla}`;");
                sb.AppendLine($"{createTableSql};\n");

                // Registros (INSERT INTO)
                using (var cmd = new MySqlCommand($"SELECT * FROM `{tabla}`", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var filas = new List<string>();
                    while (reader.Read())
                    {
                        var valores = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            if (reader.IsDBNull(i))
                            {
                                valores.Add("NULL");
                            }
                            else
                            {
                                var valor = reader.GetValue(i);
                                if (valor is bool b)
                                {
                                    valores.Add(b ? "1" : "0");
                                }
                                else if (valor is DateTime dt)
                                {
                                    valores.Add($"'{dt:yyyy-MM-dd HH:mm:ss}'");
                                }
                                else if (valor is decimal || valor is int || valor is double || valor is float)
                                {
                                    valores.Add(FormatearNumeroSql(valor));
                                }
                                else
                                {
                                    string s = valor.ToString()!.Replace("\\", "\\\\").Replace("'", "''");
                                    valores.Add($"'{s}'");
                                }
                            }
                        }
                        filas.Add($"({string.Join(", ", valores)})");

                        // Escribir en lotes de 100 para no saturar memoria
                        if (filas.Count >= 100)
                        {
                            sb.AppendLine($"INSERT INTO `{tabla}` VALUES {string.Join(",\n", filas)};");
                            filas.Clear();
                        }
                    }

                    if (filas.Count > 0)
                    {
                        sb.AppendLine($"INSERT INTO `{tabla}` VALUES {string.Join(",\n", filas)};");
                    }
                }
            }

            sb.AppendLine("\nSET FOREIGN_KEY_CHECKS = 1;");

            File.WriteAllText(rutaArchivo, sb.ToString(), Encoding.UTF8);
        }

        private static string FormatearNumeroSql(object valor)
        {
            return Convert.ToString(valor, System.Globalization.CultureInfo.InvariantCulture) ?? "0";
        }
    }
}