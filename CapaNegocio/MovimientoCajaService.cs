using System;
using System.Collections.Generic;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    public class ResumenFlujo
    {
        public decimal TotalIngresosBS { get; set; }
        public decimal TotalEgresosBS { get; set; }
        public decimal SaldoNetoBS { get; set; }
    }

    public class MovimientoCajaService
    {
        private readonly MovimientoCajaRepository _repository = new MovimientoCajaRepository();
        private readonly AuditorService _auditoria = new AuditorService();
        private readonly CajaService _cajaService = new CajaService();
        private readonly PagoService _pagoService = new PagoService();
        private readonly TasaCambioService _tasaService = new TasaCambioService();

        public List<string> ObtenerCategorias()
        {
            var categorias = new List<string> { "Pago a Profesor", "Limpieza", "Papelería", "Mantenimiento", "Servicios", "Insumos", "Otro" };
            var historicas = _repository.ObtenerCategoriasHistoricas();
            foreach (var h in historicas) if (!categorias.Contains(h)) categorias.Add(h);
            return categorias;
        }

        public List<MovimientoCaja> ObtenerTodosLosEgresos()
        {
            return _repository.ObtenerTodosLosEgresos();
        }

        public ResumenFlujo CalcularResumenGlobal()
        {
            var tasas = _tasaService.ObtenerTasasDelDia();
            decimal tasaBCV = tasas["BCV"] > 0 ? tasas["BCV"] : 1m;

            // 1. Total Histórico Ingresado (Protegido en USD directo de la BD)
            decimal ingresosTotalesUSD = _pagoService.ObtenerIngresosTotalesUSD();

            // 2. Total Histórico Gastado (Llevamos cada gasto viejo a USD)
            decimal egresosTotalesUSD = 0;
            var todosEgresos = _repository.ObtenerTodosLosEgresos();
            foreach (var eg in todosEgresos)
            {
                if (eg.MonedaCodigo == "BS")
                    egresosTotalesUSD += (eg.Monto / tasaBCV);
                // CORRECCIÓN: BCV y USD son Dólares, valen 1 a 1 en nuestra contabilidad base
                else if (eg.MonedaCodigo == "BCV" || eg.MonedaCodigo == "USD")
                    egresosTotalesUSD += eg.Monto;
                else if (eg.MonedaCodigo == "EUR" && tasas["EUR"] > 0)
                    egresosTotalesUSD += ((eg.Monto * tasas["EUR"]) / tasaBCV);
                else if (eg.MonedaCodigo == "USDT" && tasas["USDT"] > 0)
                    egresosTotalesUSD += ((eg.Monto * tasas["USDT"]) / tasaBCV);
            }

            // 3. Saldo Real de Banco (Ingresos - Gastos)
            decimal saldoNetoUSD = ingresosTotalesUSD - egresosTotalesUSD;

            return new ResumenFlujo
            {
                TotalIngresosBS = ingresosTotalesUSD * tasaBCV,
                TotalEgresosBS = egresosTotalesUSD * tasaBCV,
                SaldoNetoBS = saldoNetoUSD * tasaBCV
            };
        }

        public void RegistrarEgreso(decimal monto, string moneda, string categoria, string concepto)
        {
            var caja = _cajaService.ObtenerCajaAbierta();
            if (caja == null) throw new InvalidOperationException("No se puede registrar el egreso: La caja de hoy está cerrada.");

            var resumen = CalcularResumenGlobal();
            var tasas = _tasaService.ObtenerTasasDelDia();

            // CORRECCIÓN MATEMÁTICA: Convertimos cualquier moneda a Bolívares Reales
            decimal montoGastoBS = 0;
            if (moneda == "BS") montoGastoBS = monto;
            else if (moneda == "BCV") montoGastoBS = monto * tasas["BCV"];
            else if (moneda == "EUR") montoGastoBS = monto * tasas["EUR"];
            else if (moneda == "USDT") montoGastoBS = monto * tasas["USDT"];

            // VALIDACIÓN: No se puede gastar más de lo que tiene la academia
            if (montoGastoBS > resumen.SaldoNetoBS)
                throw new InvalidOperationException($"FONDOS INSUFICIENTES. Intenta registrar un gasto de {montoGastoBS:N2} BS, pero el sistema solo tiene {resumen.SaldoNetoBS:N2} BS líquidos disponibles.");

            string usuario = SesionService.Instancia.UsuarioActual?.Username ?? "Desconocido";
            var mov = new MovimientoCaja(caja.Id, "Egreso", monto, moneda, concepto, usuario) { Categoria = categoria };

            _repository.Registrar(mov);
            _auditoria.Registrar("INSERT", "MovimientosCaja", null, null, $"Egreso: {monto} {moneda} - {categoria}", true);
        }
    }
}