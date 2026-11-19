namespace Entidades
{
    public class InscripcionInfo
    {
        public int InscripcionId { get; set; }
        public int GrupoId { get; set; }
        public string CursoCodigo { get; set; } = string.Empty;
        public string GrupoCodigo { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string CursoNombre { get; set; } = string.Empty;
        public string GrupoNombre { get; set; } = string.Empty;
        public string Estado { get; set; } = "Activa";
        public string? Observaciones { get; set; }

        // Formato optimizado: Cédula y Nombre al inicio para búsqueda instantánea
        public string Display => $"{Cedula} - {NombreCompleto} • [{CursoNombre.ToUpper()}] ({GrupoNombre})";
    }
}