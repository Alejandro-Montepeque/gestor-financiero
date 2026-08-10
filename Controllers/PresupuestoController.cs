using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using gestor_financiero.Models;

namespace gestor_financiero.Controllers
{
    public class PresupuestoController : BaseAuthController
    {
        private readonly FinanzasContext db = new FinanzasContext();

        // GET: Presupuesto -- solo los del usuario actual
        public async Task<ActionResult> Index()
        {
            var presupuestos = db.Presupuestos
                .Where(p => p.IdUsuario == CurrentUserId)
                .OrderByDescending(p => p.Anio).ThenByDescending(p => p.Mes);
            return View(await presupuestos.ToListAsync());
        }

        // GET: Presupuesto/Details/5 -- valida que sea del usuario actual
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var presupuesto = await db.Presupuestos
                .Include(p => p.Detalles.Select(d => d.Categoria))
                .FirstOrDefaultAsync(p => p.IdPresupuesto == id && p.IdUsuario == CurrentUserId);
            if (presupuesto == null) return HttpNotFound();
            return View(presupuesto);
        }

        // GET: Presupuesto/Create
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Anio,Mes")] Presupuesto presupuesto)
        {
            // El IdUsuario lo asigna el servidor, NO se acepta del form (evita tampering)
            presupuesto.IdUsuario = CurrentUserId;

            bool existe = await db.Presupuestos.AnyAsync(p =>
                p.IdUsuario == presupuesto.IdUsuario &&
                p.Anio == presupuesto.Anio &&
                p.Mes == presupuesto.Mes);
            if (existe)
                ModelState.AddModelError("", "Ya tenés un presupuesto para ese mes/año.");

            if (ModelState.IsValid)
            {
                db.Presupuestos.Add(presupuesto);
                await db.SaveChangesAsync();
                TempData["Success"] = "Presupuesto creado correctamente.";
                return RedirectToAction("Index");
            }
            return View(presupuesto);
        }

        // GET: Presupuesto/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var presupuesto = await db.Presupuestos
                .FirstOrDefaultAsync(p => p.IdPresupuesto == id && p.IdUsuario == CurrentUserId);
            if (presupuesto == null) return HttpNotFound();
            return View(presupuesto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "IdPresupuesto,Anio,Mes")] Presupuesto presupuesto)
        {
            // Verificar primero que el registro pertenece al usuario actual
            var enBd = await db.Presupuestos
                .FirstOrDefaultAsync(p => p.IdPresupuesto == presupuesto.IdPresupuesto && p.IdUsuario == CurrentUserId);
            if (enBd == null) return HttpNotFound();

            if (ModelState.IsValid)
            {
                enBd.Anio = presupuesto.Anio;
                enBd.Mes = presupuesto.Mes;
                await db.SaveChangesAsync();
                TempData["Success"] = "Presupuesto actualizado.";
                return RedirectToAction("Index");
            }
            return View(presupuesto);
        }

        // GET: Presupuesto/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var presupuesto = await db.Presupuestos
                .FirstOrDefaultAsync(p => p.IdPresupuesto == id && p.IdUsuario == CurrentUserId);
            if (presupuesto == null) return HttpNotFound();
            return View(presupuesto);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var presupuesto = await db.Presupuestos
                .FirstOrDefaultAsync(p => p.IdPresupuesto == id && p.IdUsuario == CurrentUserId);
            if (presupuesto == null) return HttpNotFound();
            db.Presupuestos.Remove(presupuesto);
            await db.SaveChangesAsync();
            TempData["Success"] = "Presupuesto eliminado.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
