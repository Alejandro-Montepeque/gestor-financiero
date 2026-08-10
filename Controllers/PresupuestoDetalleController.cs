using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using gestor_financiero.Models;

namespace gestor_financiero.Controllers
{
    public class PresupuestoDetalleController : BaseAuthController
    {
        private readonly FinanzasContext db = new FinanzasContext();

        // GET: PresupuestoDetalle -- solo los detalles cuyo presupuesto pertenece al usuario actual
        public async Task<ActionResult> Index(int? idPresupuesto)
        {
            var query = db.PresupuestoDetalles
                .Include(d => d.Categoria)
                .Include(d => d.Presupuesto)
                .Where(d => d.Presupuesto.IdUsuario == CurrentUserId);

            if (idPresupuesto.HasValue)
                query = query.Where(d => d.IdPresupuesto == idPresupuesto.Value);

            ViewBag.IdPresupuesto = idPresupuesto;
            return View(await query.ToListAsync());
        }

        // GET: PresupuestoDetalle/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var detalle = await db.PresupuestoDetalles
                .Include(d => d.Categoria)
                .Include(d => d.Presupuesto)
                .FirstOrDefaultAsync(d => d.IdDetalle == id && d.Presupuesto.IdUsuario == CurrentUserId);
            if (detalle == null) return HttpNotFound();
            return View(detalle);
        }

        // GET: PresupuestoDetalle/Create
        public async Task<ActionResult> Create(int? idPresupuesto)
        {
            // Solo presupuestos del usuario actual en el dropdown
            var presupuestosUsuario = await db.Presupuestos
                .Where(p => p.IdUsuario == CurrentUserId)
                .OrderByDescending(p => p.Anio).ThenByDescending(p => p.Mes)
                .ToListAsync();

            ViewBag.IdPresupuesto = new SelectList(presupuestosUsuario, "IdPresupuesto", "IdPresupuesto", idPresupuesto);
            ViewBag.IdCategoria = await CategoriaHelper.ConstruirSelectAgrupado(db);
            return View(new PresupuestoDetalle { IdPresupuesto = idPresupuesto ?? 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "IdPresupuesto,IdCategoria,Estimado,Real")] PresupuestoDetalle detalle)
        {
            // Confirmar que el presupuesto destino pertenece al usuario actual
            bool perteneceAlUsuario = await db.Presupuestos
                .AnyAsync(p => p.IdPresupuesto == detalle.IdPresupuesto && p.IdUsuario == CurrentUserId);
            if (!perteneceAlUsuario)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            if (ModelState.IsValid)
            {
                db.PresupuestoDetalles.Add(detalle);
                await db.SaveChangesAsync();
                TempData["Success"] = "Detalle agregado al presupuesto.";
                return RedirectToAction("Index", new { idPresupuesto = detalle.IdPresupuesto });
            }

            var presupuestosUsuario = await db.Presupuestos
                .Where(p => p.IdUsuario == CurrentUserId).ToListAsync();
            ViewBag.IdPresupuesto = new SelectList(presupuestosUsuario, "IdPresupuesto", "IdPresupuesto", detalle.IdPresupuesto);
            ViewBag.IdCategoria = await CategoriaHelper.ConstruirSelectAgrupado(db, detalle.IdCategoria);
            return View(detalle);
        }

        // GET: PresupuestoDetalle/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var detalle = await db.PresupuestoDetalles
                .Include(d => d.Presupuesto)
                .FirstOrDefaultAsync(d => d.IdDetalle == id && d.Presupuesto.IdUsuario == CurrentUserId);
            if (detalle == null) return HttpNotFound();

            var presupuestosUsuario = await db.Presupuestos
                .Where(p => p.IdUsuario == CurrentUserId).ToListAsync();
            ViewBag.IdPresupuesto = new SelectList(presupuestosUsuario, "IdPresupuesto", "IdPresupuesto", detalle.IdPresupuesto);
            ViewBag.IdCategoria = await CategoriaHelper.ConstruirSelectAgrupado(db, detalle.IdCategoria);
            return View(detalle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "IdDetalle,IdPresupuesto,IdCategoria,Estimado,Real")] PresupuestoDetalle detalle)
        {
            // 1. Verificar que el detalle original es del usuario actual
            var enBd = await db.PresupuestoDetalles
                .Include(d => d.Presupuesto)
                .FirstOrDefaultAsync(d => d.IdDetalle == detalle.IdDetalle && d.Presupuesto.IdUsuario == CurrentUserId);
            if (enBd == null) return HttpNotFound();

            // 2. Verificar que si cambian el IdPresupuesto, el nuevo presupuesto también es suyo
            bool nuevoPerteneceAlUsuario = await db.Presupuestos
                .AnyAsync(p => p.IdPresupuesto == detalle.IdPresupuesto && p.IdUsuario == CurrentUserId);
            if (!nuevoPerteneceAlUsuario)
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            if (ModelState.IsValid)
            {
                enBd.IdPresupuesto = detalle.IdPresupuesto;
                enBd.IdCategoria = detalle.IdCategoria;
                enBd.Estimado = detalle.Estimado;
                enBd.Real = detalle.Real;
                await db.SaveChangesAsync();
                TempData["Success"] = "Detalle actualizado.";
                return RedirectToAction("Index", new { idPresupuesto = detalle.IdPresupuesto });
            }

            var presupuestosUsuario = await db.Presupuestos
                .Where(p => p.IdUsuario == CurrentUserId).ToListAsync();
            ViewBag.IdPresupuesto = new SelectList(presupuestosUsuario, "IdPresupuesto", "IdPresupuesto", detalle.IdPresupuesto);
            ViewBag.IdCategoria = await CategoriaHelper.ConstruirSelectAgrupado(db, detalle.IdCategoria);
            return View(detalle);
        }

        // GET: PresupuestoDetalle/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var detalle = await db.PresupuestoDetalles
                .Include(d => d.Categoria)
                .Include(d => d.Presupuesto)
                .FirstOrDefaultAsync(d => d.IdDetalle == id && d.Presupuesto.IdUsuario == CurrentUserId);
            if (detalle == null) return HttpNotFound();
            return View(detalle);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var detalle = await db.PresupuestoDetalles
                .Include(d => d.Presupuesto)
                .FirstOrDefaultAsync(d => d.IdDetalle == id && d.Presupuesto.IdUsuario == CurrentUserId);
            if (detalle == null) return HttpNotFound();

            int idPresupuesto = detalle.IdPresupuesto;
            db.PresupuestoDetalles.Remove(detalle);
            await db.SaveChangesAsync();
            TempData["Success"] = "Detalle eliminado.";
            return RedirectToAction("Index", new { idPresupuesto });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
