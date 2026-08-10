using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using gestor_financiero.Models;

namespace gestor_financiero
{
    /// <summary>
    /// Construye un listado de categorias agrupado por Tipo para usar en
    /// dropdowns. Asi el usuario ve las opciones organizadas por:
    ///   INGRESO / AHORRO / GASTO FIJO / GASTO VARIABLE / DEUDA
    /// con etiquetas <optgroup> que renderiza el navegador automaticamente.
    /// </summary>
    public static class CategoriaHelper
    {
        // Etiquetas amigables que se muestran como nombre de grupo en el <optgroup>
        private static readonly Dictionary<string, string> EtiquetasGrupo =
            new Dictionary<string, string>
            {
                { "INGRESO",        "Ingresos" },
                { "AHORRO",         "Ahorros" },
                { "GASTO_FIJO",     "Gastos fijos" },
                { "GASTO_VARIABLE", "Gastos variables" },
                { "DEUDA",          "Deudas" }
            };

        public static async Task<IEnumerable<SelectListItem>> ConstruirSelectAgrupado(
            FinanzasContext db, int? seleccionada = null)
        {
            var categorias = await db.Categorias
                .OrderBy(c => c.Tipo)
                .ThenBy(c => c.Nombre)
                .ToListAsync();

            // Un SelectListGroup por cada Tipo distinto (la igualdad de grupos
            // se hace por referencia, asi que reutilizamos la misma instancia
            // para todas las categorias del mismo tipo).
            var grupos = new Dictionary<string, SelectListGroup>();
            foreach (var tipo in categorias.Select(c => c.Tipo).Distinct())
            {
                string nombre;
                if (!EtiquetasGrupo.TryGetValue(tipo, out nombre)) nombre = tipo;
                grupos[tipo] = new SelectListGroup { Name = nombre };
            }

            return categorias.Select(c => new SelectListItem
            {
                Value = c.IdCategoria.ToString(),
                Text = c.Nombre,
                Group = grupos[c.Tipo],
                Selected = seleccionada.HasValue && c.IdCategoria == seleccionada.Value
            }).ToList();
        }
    }
}
