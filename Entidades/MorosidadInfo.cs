namespace Entidades
{
    public class MorosidadInfo
    {
        public string Cedula { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string Curso { get; set; } = string.Empty;
        public int ClasesImpartidas { get; set; }
        public decimal TotalPagado { get; set; }

        public decimal DeudaUSD { get; set; }
        public decimal DeudaBCV { get; set; }
        public decimal DeudaEUR { get; set; }
        public decimal DeudaUSDT { get; set; }

        // Mostrará un texto apilado muy visual y claro
        public string DeudaStr => $"USD: {DeudaUSD:N2} | BCV: {DeudaBCV:N2}\nEUR: {DeudaEUR:N2} | USDT: {DeudaUSDT:N2}";
    }
}