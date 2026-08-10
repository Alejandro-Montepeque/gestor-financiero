using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using gestor_financiero.Models;

namespace gestor_financiero.Controllers
{
    public class CategoriaController : Controller
    {
        private readonly FinanzasContext db = new FinanzasContext();

        // Lista de tipos válidos para el dropdown
        private static readonly List<string> TiposValidos = new List<string>
        {
            TipoCategoria.Ingreso,
            TipoCategoria.Ahorro,
            TipoCategoria.GastoFijo,
            TipoCategoria.GastoVariable,
            TipoCategoria.Deuda
        };

        // GET: Categoria
        public async Task<ActionResult> Index()
        {
            return View(await db.Categorias.OrderBy(c => c.Tipo).ThenBy(c => c.Nombre).ToListAsync());
        }

        // GET: Categoria/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var categoria = await db.Categorias.FindAsync(id);
            if (categoria == null) return HttpNotFound();
            return View(categoria);
        }

        // GET: Categoria/Create
        public ActionResult Create()
        {
            ViewBag.Tipo = new SelectList(TiposValidos);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create([Bind(Include = "Nombre,Tipo")] Categoria categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria.Tipo))
                ModelState.AddModelError("Tipo", "Seleccioná un tipo de categoría.");
            else if (!TiposValidos.Contains(categoria.Tipo))
                ModelState.AddModelError("Tipo", "Tipo de categoría no válido: " + categoria.Tipo);

            if (ModelState.IsValid)
            {
                db.Categorias.Add(categoria);
                await db.SaveChangesAsync();
                TempData["Success"] = "Categoría creada.";
                return RedirectToAction("Index");
            }
            ViewBag.Tipo = new SelectList(TiposValidos, categoria.Tipo);
            return View(categoria);
        }

        // GET: Categoria/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var categoria = await db.Categorias.FindAsync(id);
            if (categoria == null) return HttpNotFound();
            ViewBag.Tipo = new SelectList(TiposValidos, categoria.Tipo);
            return View(categoria);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit([Bind(Include = "IdCategoria,Nombre,Tipo")] Categoria categoria)
        {
            if (string.IsNullOrWhiteSpace(categoria.Tipo))
                ModelState.AddModelError("Tipo", "Seleccioná un tipo de categoría.");
            else if (!TiposValidos.Contains(categoria.Tipo))
                ModelState.AddModelError("Tipo", "Tipo de categoría no válido: " + categoria.Tipo);

            if (ModelState.IsValid)
            {
                db.Entry(categoria).State = EntityState.Modified;
                await db.SaveChangesAsync();
                TempData["Success"] = "Categoría actualizada.";
                return RedirectToAction("Index");
            }
            ViewBag.Tipo = new SelectList(TiposValidos, categoria.Tipo);
            return View(categoria);
        }

        // GET: Categoria/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var categoria = await db.Categorias.FindAsync(id);
            if (categoria == null) return HttpNotFound();
            return View(categoria);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var categoria = await db.Categorias.FindAsync(id);
            db.Categorias.Remove(categoria);
            await db.SaveChangesAsync();
            TempData["Success"] = "Categoría eliminada.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
