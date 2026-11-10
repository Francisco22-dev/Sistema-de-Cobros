using System;
using System.Collections.Generic;
using System.Linq;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    public class ResumenFlujo
    {
        public decimal TotalIngresosBS { get; set; }
        public decimal TotalEgresosBS { get; set; }
        public decimal SaldoNetoBS { get; set; }

        public decimal FisicoBs { get; set; }
        public decimal BancoBs { get; set; }
        public decimal FisicoUsd { get; set; }
        public decimal BancoUsd { get; set; }
    }

    public class MovimientoCajaService
    {
        private readonly MovimientoCajaRepository _repository = new MovimientoCajaRepository();
        private readonly AuditorService _auditoria = new AuditorService();
        private readonly CajaService _cajaService = new CajaService();
        private readonly PagoService _pagoService = new PagoService();
        private readonly TasaCambioService _tasaService = new TasaCambioService();
        private readonly PagoRepository _pagoRepo = new PagoRepository();

        public List<string> ObtenerCategorias()
        {
            var categorias = new List<string> { "Suministros: papelería, oficina, limpieza, cafetería interna.", "Atenciones y refrigerios.", "Equipo y material para los cursos.", "Personal: sueldos, comisiones, publicidad, etc.", "Servicios: alquiler, condominio, mantenimiento, reparación, servicios profecionales externos.", "Impuestos y trámites.", "Gastos bancarios.", "Otro." };
            var historicas = _repository.ObtenerCategoriasHistoricas();
            foreach (var h in historicas) if (!categorias.Contains(h)) categorias.Add(h);
            return categorias;
        }

        // SOLUCIÓN AL ERROR 1: Método faltante
        public List<MovimientoCaja> ObtenerTodosLosEgresos()
        {
            return _repository.ObtenerMovimientosPorTipo("Egreso");
        }

        public List<MovimientoCaja> ObtenerEgresosReales()
        {
            return _repository.ObtenerMovimientosPorTipo("Egreso").Where(e => e.Categoria != "Cambio Moneda").ToList();
        }

        public ResumenFlujo CalcularResumenGlobal()
        {
            var tasas = _tasaService.ObtenerTasasDelDia();
            decimal tasaBCV = tasas["BCV"] > 0 ? tasas["BCV"] : 1m;

            // 1. INGRESOS (Sin Cambios)
            decimal ingresosTotalesUSD = _pagoService.ObtenerIngresosTotalesUSD();
            foreach (var ing in _repository.ObtenerMovimientosPorTipo("Ingreso").Where(i => i.Categoria != "Cambio Moneda"))
            {
                if (ing.MonedaCodigo == "BS") ingresosTotalesUSD += (ing.Monto / tasaBCV);
                else if (ing.MonedaCodigo == "BCV" || ing.MonedaCodigo == "USD") ingresosTotalesUSD += ing.Monto;
            }

            // 2. EGRESOS (Sin Cambios)
            decimal egresosTotalesUSD = 0;
            foreach (var eg in _repository.ObtenerMovimientosPorTipo("Egreso").Where(e => e.Categoria != "Cambio Moneda"))
            {
                if (eg.MonedaCodigo == "BS") egresosTotalesUSD += (eg.Monto / tasaBCV);
                else if (eg.MonedaCodigo == "BCV" || eg.MonedaCodigo == "USD") egresosTotalesUSD += eg.Monto;
                else if (eg.MonedaCodigo == "EUR" && tasas["EUR"] > 0) egresosTotalesUSD += ((eg.Monto * tasas["EUR"]) / tasaBCV);
                else if (eg.MonedaCodigo == "USDT" && tasas["USDT"] > 0) egresosTotalesUSD += ((eg.Monto * tasas["USDT"]) / tasaBCV);
            }

            // 3. DESGLOSE FÍSICO/BANCO EN TIEMPO REAL
            var todosPagos = _pagoRepo.ObtenerDesglosePagosGlobal();
            var todosMovs = _repository.ObtenerDesgloseMovimientosGlobal();

            var consolidado = todosPagos.Concat(todosMovs).ToList();

            decimal fBs = consolidado.Where(a => a.Moneda == "BS" && a.Metodo.Contains("Efectivo")).Sum(a => a.Total);
            decimal bBs = consolidado.Where(a => a.Moneda == "BS" && !a.Metodo.Contains("Efectivo")).Sum(a => a.Total);
            decimal fUsd = consolidado.Where(a => (a.Moneda == "BCV" || a.Moneda == "USD" || a.Moneda == "$") && (a.Metodo.Contains("Divisas") || a.Metodo.Contains("Efectivo"))).Sum(a => a.Total);
            decimal bUsd = consolidado.Where(a => (a.Moneda == "BCV" || a.Moneda == "USD" || a.Moneda == "$") && a.Metodo.Contains("Zelle")).Sum(a => a.Total);

            // CORRECCIÓN VISUAL: Si la cuenta da negativo (sobregiro por cambios sin fondos previos), la limitamos a 0 visualmente.
            fBs = Math.Max(0, fBs);
            bBs = Math.Max(0, bBs);
            fUsd = Math.Max(0, fUsd);
            bUsd = Math.Max(0, bUsd);

            return new ResumenFlujo
            {
                TotalIngresosBS = ingresosTotalesUSD * tasaBCV,
                TotalEgresosBS = egresosTotalesUSD * tasaBCV,
                SaldoNetoBS = (ingresosTotalesUSD - egresosTotalesUSD) * tasaBCV,
                FisicoBs = fBs,
                BancoBs = bBs,
                FisicoUsd = fUsd,
                BancoUsd = bUsd
            };
        }

        public void RegistrarEgreso(decimal monto, string moneda, string categoria, string concepto, string metodo)
        {
            var caja = _cajaService.ObtenerCajaAbierta();
            if (caja == null) throw new InvalidOperationException("La caja está cerrada.");

            var resumen = CalcularResumenGlobal();
            var tasas = _tasaService.ObtenerTasasDelDia();
            decimal montoGastoBS = (moneda == "BS") ? monto : monto * tasas["BCV"];

            if (montoGastoBS > resumen.SaldoNetoBS)
                throw new InvalidOperationException($"FONDOS INSUFICIENTES. Intenta gastar {montoGastoBS:N2} BS, la caja solo tiene {resumen.SaldoNetoBS:N2} BS líquidos.");

            string usuario = SesionService.Instancia.UsuarioActual?.Username ?? "Desconocido";
            var mov = new MovimientoCaja(caja.Id, "Egreso", monto, moneda, concepto, usuario) { Categoria = categoria, Metodo = metodo };

            _repository.Registrar(mov);
            _auditoria.Registrar("INSERT", "MovimientosCaja", null, null, $"Egreso: {monto} {moneda} en {metodo}", true);
        }

        public void RegistrarCambioMoneda(decimal montoEntra, string monedaEntra, string metodoEntra, decimal montoSale, string monedaSale, string metodoSale, string concepto)
        {
            var caja = _cajaService.ObtenerCajaAbierta();
            if (caja == null) throw new InvalidOperationException("La caja de hoy está cerrada.");

            var resumen = CalcularResumenGlobal();
            var tasas = _tasaService.ObtenerTasasDelDia();
            decimal montoSaleBS = (monedaSale == "BS") ? montoSale : montoSale * tasas["BCV"];

            if (montoSaleBS > resumen.SaldoNetoBS)
                throw new InvalidOperationException("FONDOS INSUFICIENTES para entregar ese cambio.");

            string usuario = SesionService.Instancia.UsuarioActual?.Username ?? "Desconocido";

            _repository.Registrar(new MovimientoCaja(caja.Id, "Ingreso", montoEntra, monedaEntra, $"CAMBIO RECIBIDO: {concepto}", usuario) { Categoria = "Cambio Moneda", Metodo = metodoEntra });
            _repository.Registrar(new MovimientoCaja(caja.Id, "Egreso", montoSale, monedaSale, $"CAMBIO ENTREGADO: {concepto}", usuario) { Categoria = "Cambio Moneda", Metodo = metodoSale });
            _auditoria.Registrar("CAMBIO", "MovimientosCaja", null, null, $"Entró: {montoEntra} {monedaEntra} ({metodoEntra}) | Salió: {montoSale} {monedaSale} ({metodoSale})", true);
        }
    }
}