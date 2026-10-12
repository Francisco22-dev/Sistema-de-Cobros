namespace Entidades
{
    /// <summary>
    /// Representa un curso ofertado por el instituto.
    /// </summary>
    public class Curso
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public int DuracionSemanas { get; set; }
        public decimal PrecioInscripcion { get; set; }
        public decimal PrecioMensualidad { get; set; }
        public bool Activo { get; set; }

        public Curso(string codigo, string nombre, int duracionSemanas)
        {
            Codigo = codigo;
            Nombre = nombre;
            DuracionSemanas = duracionSemanas;
            Activo = true;
        }
    }
}
