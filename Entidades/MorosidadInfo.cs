namespace Entidades
{
    public class MorosidadInfo
    {
        public string Cedula { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string CursoCodigo { get; set; } = string.Empty;
        public string GrupoCodigo { get; set; } = string.Empty; // Ej: AAD2610
        public string Curso { get; set; } = string.Empty;       // Ej: Asistente Administrativo Contable (AAD2610)
        public int ClasesImpartidas { get; set; }
        public decimal TotalPagado { get; set; }

        public decimal DeudaUSD { get; set; }
        public decimal DeudaBCV { get; set; }
        public decimal DeudaEUR { get; set; }
        public decimal DeudaUSDT { get; set; }
        public decimal PrecioClase { get; set; }
        public int ClasesEnDeuda => PrecioClase > 0 ? (int)(DeudaUSD / PrecioClase) : 0;

        public string DeudaStr => $"{DeudaUSD:N2} $\n(BCV: {DeudaBCV:N2} Bs)";
        public string ColorFila => ClasesEnDeuda >= 4 ? "#FFCDD2" : (ClasesEnDeuda > 0 ? "#FFF9C4" : "White");
        public bool MostrarWhatsApp => ClasesEnDeuda >= 4;
    }
}