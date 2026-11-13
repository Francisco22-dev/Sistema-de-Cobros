using System;
using System.Collections.Generic;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    public class TasaCambioService
    {
        private readonly TasaCambioRepository _repository = new TasaCambioRepository();
        private readonly AuditorService _auditoria = new AuditorService();

        public bool EsTasaDeHoyConfigurada()
        {
            return _repository.ObtenerTasaDeHoy("BCV", "BS") != null;
        }

        public Dictionary<string, decimal> ObtenerTasasDelDia()
        {
            var tasas = new Dictionary<string, decimal> { { "BS", 1m }, { "BCV", 0m }, { "EUR", 0m }, { "USDT", 0m } };

            // Intenta cargar la de hoy; si no existe, toma la última tasa registrada
            var tasaBCV = _repository.ObtenerTasaDeHoy("BCV", "BS") ?? _repository.ObtenerUltimaTasaRegistrada("BCV", "BS");
            if (tasaBCV != null) tasas["BCV"] = tasaBCV.Tasa;

            var tasaEUR = _repository.ObtenerTasaDeHoy("EUR", "BS") ?? _repository.ObtenerUltimaTasaRegistrada("EUR", "BS");
            if (tasaEUR != null) tasas["EUR"] = tasaEUR.Tasa;

            var tasaUSDT = _repository.ObtenerTasaDeHoy("USDT", "BS") ?? _repository.ObtenerUltimaTasaRegistrada("USDT", "BS");
            if (tasaUSDT != null) tasas["USDT"] = tasaUSDT.Tasa;

            return tasas;
        }

        public void FijarTasas(decimal bcv, decimal eur, decimal usdt)
        {
            if (bcv <= 0 || eur <= 0 || usdt <= 0)
                throw new InvalidOperationException("Todas las tasas deben ser mayores a cero.");

            string usuario = SesionService.Instancia.UsuarioActual?.Username ?? "Desconocido";
            DateTime hoy = DateTime.Now.Date;

            _repository.RegistrarOActualizar(new TasaCambio(hoy, "BCV", "BS", bcv, usuario) { Fuente = "BCV" });
            _repository.RegistrarOActualizar(new TasaCambio(hoy, "EUR", "BS", eur, usuario) { Fuente = "BCV" });
            _repository.RegistrarOActualizar(new TasaCambio(hoy, "USDT", "BS", usdt, usuario) { Fuente = "Binance" });

            _auditoria.Registrar("FIJAR_TASA", "TasasCambio", null, null, $"BCV:{bcv} | EUR:{eur} | USDT:{usdt}", true);
        }
    }
}