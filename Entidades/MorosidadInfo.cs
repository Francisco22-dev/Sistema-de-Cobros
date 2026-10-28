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

        // Muestra la deuda base en USD, y su equivalente exacto convertido
        public string DeudaStr => $"Deuda: {DeudaUSD:N2} $\n(En BCV: {DeudaBCV:N2} Bs. | En EUR: {DeudaEUR:N2} € | En USDT: {DeudaUSDT:N2} USDT)";
    }
}