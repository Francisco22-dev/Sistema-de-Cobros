namespace Entidades
{
    /// <summary>
    /// Representa un concepto de pago.
    /// </summary>
    public class ConceptoPago
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }

        public ConceptoPago(string codigo, string nombre, string? descripcion = null)
        {
            Codigo = codigo;
            Nombre = nombre;
            Descripcion = descripcion;
            Activo = true;
        }
    }
}
