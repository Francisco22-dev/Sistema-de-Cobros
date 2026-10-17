namespace Entidades
{
    public class ArqueoItem
    {
        public string Metodo { get; set; } = string.Empty;
        public string Moneda { get; set; } = string.Empty;
        public decimal Total { get; set; }

        public string TotalStr => $"{Total:N2} {Moneda}";
    }
}