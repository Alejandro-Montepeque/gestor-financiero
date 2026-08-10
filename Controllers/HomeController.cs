using System;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using gestor_financiero.Models;
using gestor_financiero.ViewModels;

namespace gestor_financiero.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly FinanzasContext db = new FinanzasContext();

        public async Task<ActionResult> Index()
        {
            var userId = int.Parse(System.Web.HttpContext.Current.User.Identity.Name);
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
            var inicioAnio = new DateTime(hoy.Year, 1, 1);

            // KPIs del mes actual: ingresos / gastos
            var txDelMes = await db.Transacciones
                .Include(t => t.Categoria)
                .Where(t => t.IdUsuario == userId && t.Fecha >= inicioMes)
                .ToListAsync();

            decimal ingresos = txDelMes.Where(t => t.Categoria.Tipo == "INGRESO").Sum(t => t.Monto);
            decimal gastos = txDelMes.Where(t => t.Categoria.Tipo == "GASTO_FIJO" || t.Categoria.Tipo == "GASTO_VARIABLE").Sum(t => t.Monto);
            decimal ahorros = txDelMes.Where(t => t.Categoria.Tipo == "AHORRO").Sum(t => t.Monto);
            decimal deudaTotal = await db.Deudas.Where(d => d.IdUsuario == userId).SumAsync(d => (decimal?)d.SaldoActual) ?? 0;

            // Serie mensual de los últimos 6 meses
            var txAnio = await db.Transacciones
                .Include(t => t.Categoria)
                .Where(t => t.IdUsuario == userId && t.Fecha >= inicioAnio)
                .ToListAsync();

            var serie = new System.Collections.Generic.List<MonthlyPoint>();
            for (int i = 5; i >= 0; i--)
            {
                var d = hoy.AddMonths(-i);
                var ini = new DateTime(d.Year, d.Month, 1);
                var fin = ini.AddMonths(1);
                var tx = txAnio.Where(t => t.Fecha >= ini && t.Fecha < fin);
                serie.Add(new MonthlyPoint
                {
                    Mes = CultureInfo.GetCultureInfo("es").DateTimeFormat.GetAbbreviatedMonthName(d.Month).TrimEnd('.'),
                    Ingresos = tx.Where(t => t.Categoria.Tipo == "INGRESO").Sum(t => t.Monto),
                    Gastos = tx.Where(t => t.Categoria.Tipo == "GASTO_FIJO" || t.Categoria.Tipo == "GASTO_VARIABLE").Sum(t => t.Monto)
                });
            }

            // Gastos por categoría (mes actual)
            var porCategoria = txDelMes
                .Where(t => t.Categoria.Tipo == "GASTO_FIJO" || t.Categoria.Tipo == "GASTO_VARIABLE")
                .GroupBy(t => t.Categoria.Nombre)
                .Select(g => new CategoryShare { Categoria = g.Key, Monto = g.Sum(t => t.Monto) })
                .OrderByDescending(x => x.Monto)
                .Take(6)
                .ToList();

            var recientes = await db.Transacciones
                .Include(t => t.Categoria)
                .Where(t => t.IdUsuario == userId)
                .OrderByDescending(t => t.Fecha)
                .Take(5)
                .ToListAsync();

            var deudas = await db.Deudas
                .Where(d => d.IdUsuario == userId && d.SaldoActual > 0)
                .OrderByDescending(d => d.SaldoActual)
                .Take(5)
                .ToListAsync();

            var vm = new DashboardViewModel
            {
                TotalIngresos = ingresos,
                TotalGastos = gastos,
                TotalAhorros = ahorros,
                TotalDeudas = deudaTotal,
                TransaccionesMes = txDelMes.Count,
                SerieMensual = serie,
                GastosPorCategoria = porCategoria,
                TransaccionesRecientes = recientes,
                DeudasActivas = deudas
            };

            ViewBag.Title = "Dashboard";
            ViewBag.Subtitle = "Resumen financiero — " + hoy.ToString("MMMM yyyy", new CultureInfo("es"));
            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
