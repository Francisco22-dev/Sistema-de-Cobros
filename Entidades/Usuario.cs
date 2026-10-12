namespace Entidades
{
    /// <summary>
    /// Representa un usuario del sistema.
    /// </summary>
    public class Usuario
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public bool Activo { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        public int IntentosFallidos { get; set; }
        public bool Bloqueado { get; set; }
        public DateTime FechaRegistro { get; set; }

        public Usuario(string username, string passwordHash, string nombreCompleto, string rol)
        {
            Username = username;
            PasswordHash = passwordHash;
            NombreCompleto = nombreCompleto;
            Rol = rol;
            Activo = true;
            FechaRegistro = DateTime.Now;
        }

        public Usuario() { }
    }
}
