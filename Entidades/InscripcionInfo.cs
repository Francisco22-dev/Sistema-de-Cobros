namespace Entidades
{
    public class InscripcionInfo
    {
        public int InscripcionId { get; set; }
        public string Cedula { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string GrupoCodigo { get; set; } = string.Empty;

        // Esta propiedad es la que el buscador mostrará en pantalla
        public string Display => $"{Cedula} - {NombreCompleto} ({GrupoCodigo})";
    }
}