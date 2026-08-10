using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using gestor_financiero.Models;

namespace gestor_financiero.Controllers
{
    public class DeudaController : BaseAuthController
    {
        private readonly FinanzasContext db = new FinanzasContext();

        // GET: Deuda -- solo las deudas del usuario actual
        public async Task<ActionResult> Index()
        {
            var deudas = db.Deudas
                .Where(d => d.IdUsuario == CurrentUserId)
                .OrderByDescending(d => d.SaldoActual);
            return View(await deudas.ToListAsync());
        }

        // GET: Deuda/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var deuda = await db.Deudas
                .Include(d => d.Pagos)
                .FirstOrDefaultAsync(d => d.IdDeuda == id && d.IdUsuario == CurrentUserId);
            if (deuda == null) return HttpNotFound();
            return View(deuda);
        }

        // GET: Deuda/Create
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Nombre,SaldoActual,TasaInteres,PagoMinimo,Notas")] Deuda deuda)
        {
            deuda.IdUsuario = CurrentUserId;

            if (deuda.SaldoActual < 0)
                ModelState.AddModelError("SaldoActual", "El saldo no puede ser negativo.");

            if (ModelState.IsValid)
            {
                db.Deudas.Add(deuda);
                await db.SaveChangesAsync();
                TempData["Success"] = "Deuda registrada.";
                return RedirectToAction("Index");
            }
            return View(deuda);
        }

        // GET: Deuda/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var deuda = await db.Deudas
                .FirstOrDefaultAsync(d => d.IdDeuda == id && d.IdUsuario == CurrentUserId);
            if (deuda == null) return HttpNotFound();
            return View(deuda);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "IdDeuda,Nombre,SaldoActual,TasaInteres,PagoMinimo,Notas")] Deuda deuda)
        {
            var enBd = await db.Deudas
                .FirstOrDefaultAsync(d => d.IdDeuda == deuda.IdDeuda && d.IdUsuario == CurrentUserId);
            if (enBd == null) return HttpNotFound();

            if (ModelState.IsValid)
            {
                enBd.Nombre = deuda.Nombre;
                enBd.SaldoActual = deuda.SaldoActual;
                enBd.TasaInteres = deuda.TasaInteres;
                enBd.PagoMinimo = deuda.PagoMinimo;
                enBd.Notas = deuda.Notas;
                await db.SaveChangesAsync();
                TempData["Success"] = "Deuda actualizada.";
                return RedirectToAction("Index");
            }
            return View(deuda);
        }

        // GET: Deuda/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var deuda = await db.Deudas
                .FirstOrDefaultAsync(d => d.IdDeuda == id && d.IdUsuario == CurrentUserId);
            if (deuda == null) return HttpNotFound();
            return View(deuda);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var deuda = await db.Deudas
                .FirstOrDefaultAsync(d => d.IdDeuda == id && d.IdUsuario == CurrentUserId);
            if (deuda == null) return HttpNotFound();
            db.Deudas.Remove(deuda);
            await db.SaveChangesAsync();
            TempData["Success"] = "Deuda eliminada.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
