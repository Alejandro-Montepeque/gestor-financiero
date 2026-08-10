using System.Collections.Generic;
using gestor_financiero.Models;

namespace gestor_financiero.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalIngresos { get; set; }
        public decimal TotalGastos { get; set; }
        public decimal TotalAhorros { get; set; }
        public decimal TotalDeudas { get; set; }
        public decimal Balance => TotalIngresos - TotalGastos;
        public int TransaccionesMes { get; set; }

        public List<MonthlyPoint> SerieMensual { get; set; } = new List<MonthlyPoint>();
        public List<CategoryShare> GastosPorCategoria { get; set; } = new List<CategoryShare>();
        public List<Transaccion> TransaccionesRecientes { get; set; } = new List<Transaccion>();
        public List<Deuda> DeudasActivas { get; set; } = new List<Deuda>();
    }

    public class MonthlyPoint
    {
        public string Mes { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Gastos { get; set; }
    }

    public class CategoryShare
    {
        public string Categoria { get; set; }
        public decimal Monto { get; set; }
    }
}
