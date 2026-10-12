namespace Entidades
{
    /// <summary>
    /// Representa una moneda soportada por el sistema.
    /// </summary>
    public class Moneda
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Simbolo { get; set; }
        public bool EsPrincipal { get; set; }

        public Moneda(string codigo, string nombre, string simbolo, bool esPrincipal)
        {
            Codigo = codigo;
            Nombre = nombre;
            Simbolo = simbolo;
            EsPrincipal = esPrincipal;
        }
    }
}
