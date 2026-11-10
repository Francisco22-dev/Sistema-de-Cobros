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

        // Para el desglose en tiempo real
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
        private readonly PagoRepository _pagoRepo = new PagoRepository(); // Para el desglose

        public List<string> ObtenerCategorias()
        {
            var categorias = new List<string> { "Pago a Profesor", "Limpieza", "Papelería", "Mantenimiento", "Servicios", "Insumos", "Otro" };
            var historicas = _repository.ObtenerCategoriasHistoricas();
            foreach (var h in historicas) if (!categorias.Contains(h)) categorias.Add(h);
            return categorias;
        }

        // Filtramos para NO MOSTRAR los "Cambio Moneda" en la tabla de Egresos
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
            }

            // 3. DESGLOSE FÍSICO/BANCO EN TIEMPO REAL (Incluye Facturas + Egresos + Cambios)
            var todosPagos = _pagoRepo.ObtenerDesglosePagosGlobal();
            var todosMovs = _repository.ObtenerDesgloseMovimientosGlobal();
            
            // Unimos ambas listas para sumar
            var consolidado = todosPagos.Concat(todosMovs).ToList();

            decimal fBs = consolidado.Where(a => a.Moneda == "BS" && a.Metodo.Contains("Efectivo")).Sum(a => a.Total);
            decimal bBs = consolidado.Where(a => a.Moneda == "BS" && !a.Metodo.Contains("Efectivo")).Sum(a => a.Total);
            decimal fUsd = consolidado.Where(a => (a.Moneda == "BCV" || a.Moneda == "USD" || a.Moneda == "$") && (a.Metodo.Contains("Divisas") || a.Metodo.Contains("Efectivo"))).Sum(a => a.Total);
            decimal bUsd = consolidado.Where(a => (a.Moneda == "BCV" || a.Moneda == "USD" || a.Moneda == "$") && a.Metodo.Contains("Zelle")).Sum(a => a.Total);

            return new ResumenFlujo
            {
                TotalIngresosBS = ingresosTotalesUSD * tasaBCV,
                TotalEgresosBS = egresosTotalesUSD * tasaBCV,
                SaldoNetoBS = (ingresosTotalesUSD - egresosTotalesUSD) * tasaBCV,
                FisicoBs = fBs, BancoBs = bBs, FisicoUsd = fUsd, BancoUsd = bUsd
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

            // Se registra el Método en cada pata del cambio
            _repository.Registrar(new MovimientoCaja(caja.Id, "Ingreso", montoEntra, monedaEntra, $"CAMBIO RECIBIDO: {concepto}", usuario) { Categoria = "Cambio Moneda", Metodo = metodoEntra });
            _repository.Registrar(new MovimientoCaja(caja.Id, "Egreso", montoSale, monedaSale, $"CAMBIO ENTREGADO: {concepto}", usuario) { Categoria = "Cambio Moneda", Metodo = metodoSale });
        }
    }
}