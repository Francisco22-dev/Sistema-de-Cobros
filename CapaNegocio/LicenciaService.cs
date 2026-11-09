using System;
using CapaDatos;

namespace CapaNegocio
{
    public class LicenciaService
    {
        private readonly ConfiguracionRepository _repository = new ConfiguracionRepository();
        private const string CLAVE_MAESTRA = "MasterDev2026*"; // TU CLAVE SECRETA

        public LicenciaService()
        {
            _repository.InicializarTabla();

            // Si es la primera vez que se abre el sistema, da 1 año gratis por defecto
            if (string.IsNullOrEmpty(_repository.ObtenerValor("LicenciaValidaHasta")))
            {
                EstablecerVencimiento(DateTime.Now.AddYears(1));
            }
        }

        public DateTime ObtenerFechaVencimiento()
        {
            string? valor = _repository.ObtenerValor("LicenciaValidaHasta");
            if (DateTime.TryParse(valor, out DateTime fecha)) return fecha.Date;
            return DateTime.Now.Date.AddDays(-1); // Expirada por defecto si hay error
        }

        public int ObtenerDiasRestantes()
        {
            var diferencia = ObtenerFechaVencimiento() - DateTime.Now.Date;
            return diferencia.Days;
        }

        public void RenovarLicencia(string claveMestra, DateTime nuevaFecha)
        {
            if (claveMestra != CLAVE_MAESTRA)
                throw new InvalidOperationException("Clave maestra incorrecta. Acceso denegado.");

            EstablecerVencimiento(nuevaFecha);
        }

        private void EstablecerVencimiento(DateTime fecha)
        {
            _repository.GuardarValor("LicenciaValidaHasta", fecha.ToString("yyyy-MM-dd"));
        }
    }
}