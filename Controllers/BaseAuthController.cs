using System.Web.Mvc;

namespace gestor_financiero.Controllers
{
    /// <summary>
    /// Controlador base para acciones que requieren un usuario autenticado.
    /// Expone <see cref="CurrentUserId"/> para que los controladores filtren
    /// SIEMPRE por el usuario logueado y nunca expongan datos de otros usuarios.
    /// El AccountController guarda el IdUsuario en User.Identity.Name al loguearse.
    /// </summary>
    [Authorize]
    public abstract class BaseAuthController : Controller
    {
        protected int CurrentUserId
        {
            get { return int.Parse(User.Identity.Name); }
        }
    }
}
