using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using gestor_financiero.Models;

namespace gestor_financiero.Controllers
{
    public class PagoDeudaController : BaseAuthController
    {
        private readonly FinanzasContext db = new FinanzasContext();

        // GET: PagoDeuda -- solo pagos de deudas del usuario actual
        public async Task<ActionResult> Index(int? idDeuda)
        {
            var query = db.PagosDeuda
                .Include(p => p.Deuda)
                .Where(p => p.Deuda.IdUsuario == CurrentUserId);

            if (idDeuda.HasValue) query = query.Where(p => p.IdDeuda == idDeuda.Value);

            ViewBag.IdDeuda = idDeuda;
            return View(await query.OrderByDescending(p => p.Fecha).ToListAsync());
        }

        // GET: PagoDeuda/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var pago = await db.PagosDeuda
                .Include(p => p.Deuda)
                .FirstOrDefaultAsync(p => p.IdPago == id && p.Deuda.IdUsuario == CurrentUserId);
            if (pago == null) return HttpNotFound();
            return View(pago);
        }

        // GET: PagoDeuda/Create
        public async Task<ActionResult> Create(int? idDeuda)
        {
            // Pasamos la lista completa (no SelectList) para que la vista pueda
            // poner data-saldo, data-tasa, data-min en cada <option> y el JS
            // pueda autocalcular interés / capital / saldo restante.
            var deudasUsuario = await db.Deudas
                .Where(d => d.IdUsuario == CurrentUserId)
                .OrderBy(d => d.Nombre)
                .ToListAsync();
            ViewBag.DeudasUsuario = deudasUsuario;

            var deudaPreseleccionada = idDeuda.HasValue
                ? deudasUsuario.FirstOrDefault(d => d.IdDeuda == idDeuda.Value)
                : null;

            return View(new PagoDeuda
            {
                Fecha = DateTime.Today,
                IdDeuda = idDeuda ?? 0,
                // Pre-llenar el pago minimo con el de la deuda si viene una seleccionada
                PagoMinimo = deudaPreseleccionada?.PagoMinimo ?? 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "IdDeuda,Fecha,PagoMinimo,PagoExtra,Interes,Capital,SaldoRestante")] PagoDeuda pago)
        {
            // Confirmar que la deuda destino es del usuario actual
            var deuda = await db.Deudas
                .FirstOrDefaultAsync(d => d.IdDeuda == pago.IdDeuda && d.IdUsuario == CurrentUserId);
            if (deuda == null) return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            if (ModelState.IsValid)
            {
                // Si no viene saldo_restante, recalcular contra el saldo actual de la deuda
                if (!pago.SaldoRestante.HasValue)
                    pago.SaldoRestante = deuda.SaldoActual - pago.Capital;

                db.PagosDeuda.Add(pago);

                // Actualizar saldo actual de la deuda
                deuda.SaldoActual = pago.SaldoRestante.Value;
                db.Entry(deuda).State = EntityState.Modified;

                await db.SaveChangesAsync();
                TempData["Success"] = "Pago registrado.";
                return RedirectToAction("Index", new { idDeuda = pago.IdDeuda });
            }

            var deudasUsuario = await db.Deudas.Where(d => d.IdUsuario == CurrentUserId).ToListAsync();
            ViewBag.IdDeuda = new SelectList(deudasUsuario, "IdDeuda", "Nombre", pago.IdDeuda);
            return View(pago);
        }

        // GET: PagoDeuda/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var pago = await db.PagosDeuda
                .Include(p => p.Deuda)
                .FirstOrDefaultAsync(p => p.IdPago == id && p.Deuda.IdUsuario == CurrentUserId);
            if (pago == null) return HttpNotFound();

            var deudasUsuario = await db.Deudas.Where(d => d.IdUsuario == CurrentUserId).ToListAsync();
            ViewBag.IdDeuda = new SelectList(deudasUsuario, "IdDeuda", "Nombre", pago.IdDeuda);
            return View(pago);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "IdPago,IdDeuda,Fecha,PagoMinimo,PagoExtra,Interes,Capital,SaldoRestante")] PagoDeuda pago)
        {
            // 1. Pago original es del usuario
            var enBd = await db.PagosDeuda
                .Include(p => p.Deuda)
                .FirstOrDefaultAsync(p => p.IdPago == pago.IdPago && p.Deuda.IdUsuario == CurrentUserId);
            if (enBd == null) return HttpNotFound();

            // 2. Si cambian la deuda, la nueva también es del usuario
            bool nuevaPerteneceAlUsuario = await db.Deudas
                .AnyAsync(d => d.IdDeuda == pago.IdDeuda && d.IdUsuario == CurrentUserId);
            if (!nuevaPerteneceAlUsuario) return new HttpStatusCodeResult(HttpStatusCode.Forbidden);

            if (ModelState.IsValid)
            {
                enBd.IdDeuda = pago.IdDeuda;
                enBd.Fecha = pago.Fecha;
                enBd.PagoMinimo = pago.PagoMinimo;
                enBd.PagoExtra = pago.PagoExtra;
                enBd.Interes = pago.Interes;
                enBd.Capital = pago.Capital;
                enBd.SaldoRestante = pago.SaldoRestante;
                await db.SaveChangesAsync();
                TempData["Success"] = "Pago actualizado.";
                return RedirectToAction("Index", new { idDeuda = pago.IdDeuda });
            }

            var deudasUsuario = await db.Deudas.Where(d => d.IdUsuario == CurrentUserId).ToListAsync();
            ViewBag.IdDeuda = new SelectList(deudasUsuario, "IdDeuda", "Nombre", pago.IdDeuda);
            return View(pago);
        }

        // GET: PagoDeuda/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var pago = await db.PagosDeuda
                .Include(p => p.Deuda)
                .FirstOrDefaultAsync(p => p.IdPago == id && p.Deuda.IdUsuario == CurrentUserId);
            if (pago == null) return HttpNotFound();
            return View(pago);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var pago = await db.PagosDeuda
                .Include(p => p.Deuda)
                .FirstOrDefaultAsync(p => p.IdPago == id && p.Deuda.IdUsuario == CurrentUserId);
            if (pago == null) return HttpNotFound();

            int idDeuda = pago.IdDeuda;
            db.PagosDeuda.Remove(pago);
            await db.SaveChangesAsync();
            TempData["Success"] = "Pago eliminado.";
            return RedirectToAction("Index", new { idDeuda });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
