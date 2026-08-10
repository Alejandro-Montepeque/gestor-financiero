using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using gestor_financiero.Models;

namespace gestor_financiero.Controllers
{
    public class ResumenAnualController : BaseAuthController
    {
        private readonly FinanzasContext db = new FinanzasContext();

        // GET: ResumenAnual -- solo del usuario actual
        public async Task<ActionResult> Index()
        {
            var resumenes = db.ResumenesAnuales
                .Where(r => r.IdUsuario == CurrentUserId)
                .OrderByDescending(r => r.Anio);
            return View(await resumenes.ToListAsync());
        }

        // GET: ResumenAnual/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var resumen = await db.ResumenesAnuales
                .FirstOrDefaultAsync(r => r.IdResumen == id && r.IdUsuario == CurrentUserId);
            if (resumen == null) return HttpNotFound();
            return View(resumen);
        }

        // GET: ResumenAnual/Create
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Anio,Ingresos,Ahorros,GastosFijos,GastosVariables,Deudas")] ResumenAnual resumen)
        {
            resumen.IdUsuario = CurrentUserId;

            bool existe = await db.ResumenesAnuales.AnyAsync(r =>
                r.IdUsuario == resumen.IdUsuario && r.Anio == resumen.Anio);
            if (existe)
                ModelState.AddModelError("", "Ya tenés un resumen para ese año.");

            if (ModelState.IsValid)
            {
                db.ResumenesAnuales.Add(resumen);
                await db.SaveChangesAsync();
                TempData["Success"] = "Resumen anual creado.";
                return RedirectToAction("Index");
            }
            return View(resumen);
        }

        // GET: ResumenAnual/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var resumen = await db.ResumenesAnuales
                .FirstOrDefaultAsync(r => r.IdResumen == id && r.IdUsuario == CurrentUserId);
            if (resumen == null) return HttpNotFound();
            return View(resumen);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "IdResumen,Anio,Ingresos,Ahorros,GastosFijos,GastosVariables,Deudas")] ResumenAnual resumen)
        {
            var enBd = await db.ResumenesAnuales
                .FirstOrDefaultAsync(r => r.IdResumen == resumen.IdResumen && r.IdUsuario == CurrentUserId);
            if (enBd == null) return HttpNotFound();

            if (ModelState.IsValid)
            {
                enBd.Anio = resumen.Anio;
                enBd.Ingresos = resumen.Ingresos;
                enBd.Ahorros = resumen.Ahorros;
                enBd.GastosFijos = resumen.GastosFijos;
                enBd.GastosVariables = resumen.GastosVariables;
                enBd.Deudas = resumen.Deudas;
                await db.SaveChangesAsync();
                TempData["Success"] = "Resumen anual actualizado.";
                return RedirectToAction("Index");
            }
            return View(resumen);
        }

        // GET: ResumenAnual/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var resumen = await db.ResumenesAnuales
                .FirstOrDefaultAsync(r => r.IdResumen == id && r.IdUsuario == CurrentUserId);
            if (resumen == null) return HttpNotFound();
            return View(resumen);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var resumen = await db.ResumenesAnuales
                .FirstOrDefaultAsync(r => r.IdResumen == id && r.IdUsuario == CurrentUserId);
            if (resumen == null) return HttpNotFound();
            db.ResumenesAnuales.Remove(resumen);
            await db.SaveChangesAsync();
            TempData["Success"] = "Resumen anual eliminado.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
