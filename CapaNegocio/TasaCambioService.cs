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

        public Dictionary<string, decimal> ObtenerTasasDelDia()
        {
            var tasas = new Dictionary<string, decimal> { { "BS", 1m }, { "BCV", 0m }, { "EUR", 0m }, { "USDT", 0m } };

            var tasaBCV = _repository.ObtenerTasaDelDia("BCV", "BS");
            if (tasaBCV != null) tasas["BCV"] = tasaBCV.Tasa;

            var tasaEUR = _repository.ObtenerTasaDelDia("EUR", "BS");
            if (tasaEUR != null) tasas["EUR"] = tasaEUR.Tasa;

            var tasaUSDT = _repository.ObtenerTasaDelDia("USDT", "BS");
            if (tasaUSDT != null) tasas["USDT"] = tasaUSDT.Tasa;

            return tasas;
        }

        public void FijarTasas(decimal bcv, decimal eur, decimal usdt)
        {
            if (bcv <= 0 || eur <= 0 || usdt <= 0)
                throw new InvalidOperationException("Todas las tasas deben ser mayores a cero.");

            string usuario = SesionService.Instancia.UsuarioActual?.Username ?? "Desconocido";
            DateTime hoy = DateTime.Now.Date;

            if (_repository.ObtenerTasaDelDia("BCV", "BS") == null)
            {
                _repository.Registrar(new TasaCambio(hoy, "BCV", "BS", bcv, usuario) { Fuente = "BCV" });
                _repository.Registrar(new TasaCambio(hoy, "EUR", "BS", eur, usuario) { Fuente = "BCV" });
                _repository.Registrar(new TasaCambio(hoy, "USDT", "BS", usdt, usuario) { Fuente = "Binance" });

                _auditoria.Registrar("NUEVA TASA", "TasasCambio", null, null, $"BCV:{bcv} | EUR:{eur} | USDT:{usdt}", true);
            }
        }
    }
}