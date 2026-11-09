using System;
using System.Diagnostics;
using MySqlConnector;

namespace CapaDatos
{
    public class MantenimientoRepository
    {
        public void OptimizarTablas()
        {
            using var conn = DatabaseConnection.AbrirConexionConReintento();
            // Desfragmenta y repara los índices de todas las tablas pesadas sin borrar datos
            using var cmd = new MySqlCommand("OPTIMIZE TABLE Pagos, PagoDetalles, Inscripciones, Estudiantes, Auditoria, MovimientosCaja", conn);
            cmd.CommandTimeout = 120; // Le damos 2 minutos por si la base de datos es muy grande
            cmd.ExecuteNonQuery();
        }

        public void RespaldarBaseDeDatos(string rutaArchivo)
        {
            // Construimos el comando nativo de MariaDB (mysqldump)
            string user = DatabaseConfig.UserId;
            string pass = DatabaseConfig.Password;
            string server = DatabaseConfig.Server;
            string port = DatabaseConfig.Port;
            string db = DatabaseConfig.Database;

            string dumpCommand = $"mysqldump -h {server} -P {port} -u {user} --password=\"{pass}\" {db} --result-file=\"{rutaArchivo}\"";

            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {dumpCommand}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            process?.WaitForExit();

            if (process?.ExitCode != 0)
            {
                string error = process?.StandardError.ReadToEnd() ?? "Error desconocido.";
                throw new Exception($"Fallo al crear respaldo. Verifique que 'mysqldump' esté instalado en este equipo.\nDetalle: {error}");
            }
        }
    }
}