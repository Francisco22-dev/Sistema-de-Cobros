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

        // FORMATO CLARO: Muestra la base en Dólares ($) y las conversiones exactas con sus símbolos
        public string DeudaStr => $"{DeudaUSD:N2} $\n(BCV: {DeudaBCV:N2} Bs | EUR: {DeudaEUR:N2} € | USDT: {DeudaUSDT:N2} ₮)";
    }
}