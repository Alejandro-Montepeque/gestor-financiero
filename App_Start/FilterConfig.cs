using System.Web.Mvc;

namespace gestor_financiero
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            // Todos los controladores requieren login por defecto.
            // Las acciones marcadas con [AllowAnonymous] (Login, Register) quedan accesibles.
            filters.Add(new AuthorizeAttribute());
            filters.Add(new HandleErrorAttribute());
        }
    }
}
