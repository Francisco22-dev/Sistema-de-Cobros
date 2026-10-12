using System;
using System.Threading;
using MySqlConnector;

namespace CapaDatos
{
    /// <summary>
    /// Gestiona la conexión a la base de datos MariaDB.
    /// Incluye pooling, reintentos y timeout para red local.
    /// </summary>
    public class DatabaseConnection
    {
        private const int MAX_REINTENTOS = 3;
        private const int SEGUNDOS_ENTRE_REINTENTOS = 2;
        private static readonly object _lock = new object();

        /// <summary>
        /// Crea y retorna una nueva conexión a la base de datos.
        /// Pooling activado para soportar múltiples clientes simultáneos.
        /// </summary>
        public static MySqlConnection ObtenerConexion()
        {
            string connStr = DatabaseConfig.ConnectionString +
                ";Pooling=true;Min Pool Size=1;Max Pool Size=20;" +
                "Connection Timeout=15;Default Command Timeout=30;" +
                "ConnectionLifeTime=300;";
            return new MySqlConnection(connStr);
        }

        /// <summary>
        /// Abre una conexión con reintentos. Lanza excepción si no logra conectarse.
        /// </summary>
        public static MySqlConnection AbrirConexionConReintento()
        {
            Exception? ultimaExcepcion = null;

            for (int i = 0; i < MAX_REINTENTOS; i++)
            {
                try
                {
                    var conn = ObtenerConexion();
                    conn.Open();
                    return conn;
                }
                catch (Exception ex)
                {
                    ultimaExcepcion = ex;
                    if (i < MAX_REINTENTOS - 1)
                        Thread.Sleep(SEGUNDOS_ENTRE_REINTENTOS * 1000);
                }
            }

            throw new InvalidOperationException(
                $"No se pudo conectar a la base de datos después de {MAX_REINTENTOS} intentos. " +
                "Verifique que el servidor MariaDB esté activo y la red local funcione.",
                ultimaExcepcion ?? new Exception("Sin detalles"));
        }

        /// <summary>
        /// Verifica que la conexión a la base de datos funcione.
        /// Lanza una excepción si no se puede conectar.
        /// </summary>
        public static void VerificarConexion()
        {
            using var conn = AbrirConexionConReintento();
        }
    }
}
