using System.Configuration;

namespace CapaDatos
{
    /// <summary>
    /// Configuración de la base de datos desde App.config.
    /// </summary>
    public static class DatabaseConfig
    {
        public static string Server => ConfigurationManager.AppSettings["DbServer"] ?? "localhost";
        public static string Port => ConfigurationManager.AppSettings["DbPort"] ?? "3306";
        public static string Database => ConfigurationManager.AppSettings["DbName"] ?? "sistema_cobros";
        public static string UserId => ConfigurationManager.AppSettings["DbUser"] ?? "root";
        public static string Password => ConfigurationManager.AppSettings["DbPassword"] ?? "";
        public static string NombreComputadora => ConfigurationManager.AppSettings["NombreComputadora"] ?? "PC-Principal";

        public static string ConnectionString =>
            $"Server={Server};Port={Port};Database={Database};User Id={UserId};Password={Password};";
    }
}
