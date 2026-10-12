namespace Entidades
{
    /// <summary>
    /// Representa un método de pago disponible.
    /// </summary>
    public class MetodoPago
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public bool Activo { get; set; }

        public MetodoPago(string codigo, string nombre)
        {
            Codigo = codigo;
            Nombre = nombre;
            Activo = true;
        }
    }
}
