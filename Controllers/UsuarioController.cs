using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using gestor_financiero.Models;
using gestor_financiero.ViewModels;

namespace gestor_financiero.Controllers
{
    /// <summary>
    /// Controlador de "Mi perfil". NO administra todos los usuarios.
    /// El usuario solo puede ver y editar sus propios datos.
    /// Para borrar la cuenta o cambiar contraseña: AccountController.
    /// </summary>
    public class UsuarioController : BaseAuthController
    {
        private readonly FinanzasContext db = new FinanzasContext();

        // GET: Usuario  -> Mi perfil
        public async Task<ActionResult> Index()
        {
            var miUsuario = await db.Usuarios.FindAsync(CurrentUserId);
            if (miUsuario == null) return HttpNotFound();
            return View(miUsuario);
        }

        // GET: Usuario/Details
        public async Task<ActionResult> Details()
        {
            var miUsuario = await db.Usuarios.FindAsync(CurrentUserId);
            if (miUsuario == null) return HttpNotFound();
            return View(miUsuario);
        }

        // GET: Usuario/Edit -> editar mi perfil
        public async Task<ActionResult> Edit()
        {
            var miUsuario = await db.Usuarios.FindAsync(CurrentUserId);
            if (miUsuario == null) return HttpNotFound();
            var vm = new ProfileEditViewModel
            {
                Nombre = miUsuario.Nombre,
                Email = miUsuario.Email
            };
            return View(vm);
        }

        // POST: Usuario/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(ProfileEditViewModel form)
        {
            if (!ModelState.IsValid) return View(form);

            var miUsuario = await db.Usuarios.FindAsync(CurrentUserId);
            if (miUsuario == null) return HttpNotFound();

            // Email único (si lo cambian, verificar que no exista otro con el mismo)
            if (!string.Equals(miUsuario.Email, form.Email, System.StringComparison.OrdinalIgnoreCase))
            {
                bool yaExiste = await db.Usuarios
                    .AnyAsync(u => u.Email == form.Email && u.IdUsuario != CurrentUserId);
                if (yaExiste)
                {
                    ModelState.AddModelError("Email", "Ya existe otra cuenta con ese correo.");
                    return View(form);
                }
            }

            miUsuario.Nombre = form.Nombre;
            miUsuario.Email = form.Email;
            await db.SaveChangesAsync();

            // Actualizar el nombre que se muestra en el sidebar
            Session["UserName"] = miUsuario.Nombre;

            TempData["Success"] = "Perfil actualizado correctamente.";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
