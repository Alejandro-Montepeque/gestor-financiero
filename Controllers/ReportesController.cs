using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using gestor_financiero.Models;

// NOTA: Para que las acciones que retornan PDF funcionen, instalar Rotativa via NuGet:
//   Install-Package Rotativa -Version 1.7.3
// Una vez instalado, descomentar los "using Rotativa;" y las acciones marcadas como PDF
// (las versiones "Html" siguen funcionando sin Rotativa, son vistas imprimibles).

namespace gestor_financiero.Controllers
{
    public class ReportesController : BaseAuthController
    {
        private readonly FinanzasContext db = new FinanzasContext();

        public ActionResult Index() => View();

        // -- Reporte de Transacciones (solo del usuario actual) --
        public async Task<ActionResult> Transacciones(DateTime? desde, DateTime? hasta)
        {
            var d = desde ?? new DateTime(DateTime.Today.Year, 1, 1);
            var h = hasta ?? DateTime.Today;
            var tx = await db.Transacciones
                .Include(t => t.Categoria)
                .Where(t => t.IdUsuario == CurrentUserId && t.Fecha >= d && t.Fecha <= h)
                .OrderBy(t => t.Fecha)
                .ToListAsync();
            ViewBag.Desde = d; ViewBag.Hasta = h;
            return View(tx);
        }

        // -- Reporte de Deudas (solo del usuario actual) --
        public async Task<ActionResult> Deudas()
        {
            var deudas = await db.Deudas
                .Include(d => d.Pagos)
                .Where(d => d.IdUsuario == CurrentUserId)
                .ToListAsync();
            return View(deudas);
        }

        // -- Reporte de Presupuesto (solo de presupuestos del usuario actual) --
        public async Task<ActionResult> Presupuesto(int? id)
        {
            if (!id.HasValue)
            {
                var primero = await db.Presupuestos
                    .Where(x => x.IdUsuario == CurrentUserId)
                    .OrderByDescending(x => x.Anio)
                    .ThenByDescending(x => x.Mes)
                    .FirstOrDefaultAsync();
                if (primero == null) return RedirectToAction("Index");
                id = primero.IdPresupuesto;
            }
            var presupuesto = await db.Presupuestos
                .Include(x => x.Detalles.Select(det => det.Categoria))
                .FirstOrDefaultAsync(x => x.IdPresupuesto == id && x.IdUsuario == CurrentUserId);
            if (presupuesto == null) return HttpNotFound();
            return View(presupuesto);
        }

        // -- Reporte Resumen Anual (solo del usuario actual) --
        public async Task<ActionResult> ResumenAnual()
        {
            var resumen = await db.ResumenesAnuales
                .Where(r => r.IdUsuario == CurrentUserId)
                .OrderByDescending(r => r.Anio)
                .ToListAsync();
            return View(resumen);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
