using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using gestor_financiero.Models;

namespace gestor_financiero.Controllers
{
    public class TransaccionController : BaseAuthController
    {
        private readonly FinanzasContext db = new FinanzasContext();

        // GET: Transaccion -- solo transacciones del usuario actual
        public async Task<ActionResult> Index(DateTime? desde, DateTime? hasta)
        {
            var query = db.Transacciones
                .Include(t => t.Categoria)
                .Where(t => t.IdUsuario == CurrentUserId);

            if (desde.HasValue) query = query.Where(t => t.Fecha >= desde.Value);
            if (hasta.HasValue) query = query.Where(t => t.Fecha <= hasta.Value);

            ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");

            return View(await query.OrderByDescending(t => t.Fecha).ToListAsync());
        }

        // GET: Transaccion/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var transaccion = await db.Transacciones
                .Include(t => t.Categoria)
                .FirstOrDefaultAsync(t => t.IdTransaccion == id && t.IdUsuario == CurrentUserId);
            if (transaccion == null) return HttpNotFound();
            return View(transaccion);
        }

        // GET: Transaccion/Create
        public async Task<ActionResult> Create()
        {
            ViewBag.IdCategoria = await CategoriaHelper.ConstruirSelectAgrupado(db);
            return View(new Transaccion { Fecha = DateTime.Today });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "IdCategoria,Monto,Fecha,Notas")] Transaccion transaccion)
        {
            // IdUsuario lo asigna el servidor
            transaccion.IdUsuario = CurrentUserId;

            if (transaccion.Monto <= 0)
                ModelState.AddModelError("Monto", "El monto debe ser mayor que cero.");

            if (ModelState.IsValid)
            {
                db.Transacciones.Add(transaccion);
                await db.SaveChangesAsync();
                TempData["Success"] = "Transacción registrada.";
                return RedirectToAction("Index");
            }
            ViewBag.IdCategoria = await CategoriaHelper.ConstruirSelectAgrupado(db, transaccion.IdCategoria);
            return View(transaccion);
        }

        // GET: Transaccion/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var transaccion = await db.Transacciones
                .FirstOrDefaultAsync(t => t.IdTransaccion == id && t.IdUsuario == CurrentUserId);
            if (transaccion == null) return HttpNotFound();
            ViewBag.IdCategoria = await CategoriaHelper.ConstruirSelectAgrupado(db, transaccion.IdCategoria);
            return View(transaccion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "IdTransaccion,IdCategoria,Monto,Fecha,Notas")] Transaccion transaccion)
        {
            var enBd = await db.Transacciones
                .FirstOrDefaultAsync(t => t.IdTransaccion == transaccion.IdTransaccion && t.IdUsuario == CurrentUserId);
            if (enBd == null) return HttpNotFound();

            if (ModelState.IsValid)
            {
                enBd.IdCategoria = transaccion.IdCategoria;
                enBd.Monto = transaccion.Monto;
                enBd.Fecha = transaccion.Fecha;
                enBd.Notas = transaccion.Notas;
                await db.SaveChangesAsync();
                TempData["Success"] = "Transacción actualizada.";
                return RedirectToAction("Index");
            }
            ViewBag.IdCategoria = await CategoriaHelper.ConstruirSelectAgrupado(db, transaccion.IdCategoria);
            return View(transaccion);
        }

        // GET: Transaccion/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var transaccion = await db.Transacciones
                .Include(t => t.Categoria)
                .FirstOrDefaultAsync(t => t.IdTransaccion == id && t.IdUsuario == CurrentUserId);
            if (transaccion == null) return HttpNotFound();
            return View(transaccion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var transaccion = await db.Transacciones
                .FirstOrDefaultAsync(t => t.IdTransaccion == id && t.IdUsuario == CurrentUserId);
            if (transaccion == null) return HttpNotFound();
            db.Transacciones.Remove(transaccion);
            await db.SaveChangesAsync();
            TempData["Success"] = "Transacción eliminada.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
