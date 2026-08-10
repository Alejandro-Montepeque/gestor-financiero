using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Helpers;
using gestor_financiero.Models;

namespace gestor_financiero
{
    /// <summary>
    /// Seeder de datos de prueba. Se ejecuta una sola vez al arrancar la app.
    /// Si ya hay usuarios en la BD no hace nada (idempotente).
    /// </summary>
    public static class DataSeeder
    {
        public const string DemoEmail = "demo@gestor.com";
        public const string DemoPassword = "Demo2026!";

        public static void Seed()
        {
            try
            {
                using (var db = new FinanzasContext())
                {
                    // Si ya hay usuarios, no hacemos nada
                    if (db.Usuarios.Any()) return;

                    // 1. Usuario demo con password hasheado correctamente
                    var demo = new Usuario
                    {
                        Nombre = "Usuario Demo",
                        Email = DemoEmail,
                        PasswordHash = Crypto.HashPassword(DemoPassword),
                        FechaRegistro = DateTime.Now
                    };
                    db.Usuarios.Add(demo);
                    db.SaveChanges();

                    // 2. Si no hay categorías (por alguna razón), no podemos sembrar más
                    var categorias = db.Categorias.ToList();
                    if (!categorias.Any()) return;

                    // Helpers para encontrar categorías por nombre/tipo
                    Categoria CatPorTipo(string tipo) => categorias.FirstOrDefault(c => c.Tipo == tipo);
                    Categoria CatPorNombre(string nombre) => categorias.FirstOrDefault(c => c.Nombre.Contains(nombre));

                    var hoy = DateTime.Today;

                    // 3. Algunas transacciones del mes actual
                    var catSalario = CatPorNombre("Salario") ?? CatPorTipo("INGRESO");
                    var catAlquiler = CatPorNombre("Alquiler") ?? CatPorTipo("GASTO_FIJO");
                    var catServicios = CatPorNombre("Servicios") ?? CatPorTipo("GASTO_FIJO");
                    var catSuper = CatPorNombre("Supermercado") ?? CatPorTipo("GASTO_VARIABLE");
                    var catEntret = CatPorNombre("Entretenimiento") ?? CatPorTipo("GASTO_VARIABLE");
                    var catAhorro = CatPorNombre("Emergencia") ?? CatPorTipo("AHORRO");

                    if (catSalario != null)
                        db.Transacciones.Add(new Transaccion { IdUsuario = demo.IdUsuario, IdCategoria = catSalario.IdCategoria, Monto = 1500m, Fecha = new DateTime(hoy.Year, hoy.Month, 1), Notas = "Salario mensual" });
                    if (catAlquiler != null)
                        db.Transacciones.Add(new Transaccion { IdUsuario = demo.IdUsuario, IdCategoria = catAlquiler.IdCategoria, Monto = 450m, Fecha = new DateTime(hoy.Year, hoy.Month, 2), Notas = "Alquiler del mes" });
                    if (catServicios != null)
                        db.Transacciones.Add(new Transaccion { IdUsuario = demo.IdUsuario, IdCategoria = catServicios.IdCategoria, Monto = 135.50m, Fecha = new DateTime(hoy.Year, hoy.Month, 5), Notas = "Agua, luz, internet" });
                    if (catSuper != null)
                    {
                        db.Transacciones.Add(new Transaccion { IdUsuario = demo.IdUsuario, IdCategoria = catSuper.IdCategoria, Monto = 180m, Fecha = new DateTime(hoy.Year, hoy.Month, 8), Notas = "Compra quincenal" });
                        db.Transacciones.Add(new Transaccion { IdUsuario = demo.IdUsuario, IdCategoria = catSuper.IdCategoria, Monto = 140m, Fecha = hoy.AddDays(-3), Notas = "Compras del fin de semana" });
                    }
                    if (catEntret != null)
                        db.Transacciones.Add(new Transaccion { IdUsuario = demo.IdUsuario, IdCategoria = catEntret.IdCategoria, Monto = 45m, Fecha = hoy.AddDays(-5), Notas = "Cine con amigos" });
                    if (catAhorro != null)
                        db.Transacciones.Add(new Transaccion { IdUsuario = demo.IdUsuario, IdCategoria = catAhorro.IdCategoria, Monto = 200m, Fecha = new DateTime(hoy.Year, hoy.Month, 15), Notas = "Aporte al fondo de emergencia" });

                    // 4. Presupuesto del mes actual con detalle
                    var presupuesto = new Presupuesto { IdUsuario = demo.IdUsuario, Anio = hoy.Year, Mes = hoy.Month };
                    db.Presupuestos.Add(presupuesto);
                    db.SaveChanges();

                    if (catSalario != null)
                        db.PresupuestoDetalles.Add(new PresupuestoDetalle { IdPresupuesto = presupuesto.IdPresupuesto, IdCategoria = catSalario.IdCategoria, Estimado = 1500m, Real = 1500m });
                    if (catAlquiler != null)
                        db.PresupuestoDetalles.Add(new PresupuestoDetalle { IdPresupuesto = presupuesto.IdPresupuesto, IdCategoria = catAlquiler.IdCategoria, Estimado = 450m, Real = 450m });
                    if (catServicios != null)
                        db.PresupuestoDetalles.Add(new PresupuestoDetalle { IdPresupuesto = presupuesto.IdPresupuesto, IdCategoria = catServicios.IdCategoria, Estimado = 120m, Real = 135.50m });
                    if (catSuper != null)
                        db.PresupuestoDetalles.Add(new PresupuestoDetalle { IdPresupuesto = presupuesto.IdPresupuesto, IdCategoria = catSuper.IdCategoria, Estimado = 300m, Real = 320m });
                    if (catEntret != null)
                        db.PresupuestoDetalles.Add(new PresupuestoDetalle { IdPresupuesto = presupuesto.IdPresupuesto, IdCategoria = catEntret.IdCategoria, Estimado = 80m, Real = 45m });

                    // 5. Dos deudas con sus pagos
                    var deudaVisa = new Deuda
                    {
                        IdUsuario = demo.IdUsuario,
                        Nombre = "Tarjeta de Credito Visa",
                        SaldoActual = 1169.27m,
                        TasaInteres = 18.50m,
                        PagoMinimo = 45m,
                        Notas = "Compra de laptop a cuotas"
                    };
                    var deudaAuto = new Deuda
                    {
                        IdUsuario = demo.IdUsuario,
                        Nombre = "Prestamo Automotriz",
                        SaldoActual = 8315.52m,
                        TasaInteres = 9.25m,
                        PagoMinimo = 250m,
                        Notas = "Toyota Corolla 2020"
                    };
                    db.Deudas.Add(deudaVisa);
                    db.Deudas.Add(deudaAuto);
                    db.SaveChanges();

                    // Historial de pagos a la Visa
                    db.PagosDeuda.Add(new PagoDeuda
                    {
                        IdDeuda = deudaVisa.IdDeuda,
                        Fecha = hoy.AddMonths(-1),
                        PagoMinimo = 45m, PagoExtra = 55m,
                        Interes = 19.27m, Capital = 80.73m,
                        SaldoRestante = 1169.27m
                    });
                    db.PagosDeuda.Add(new PagoDeuda
                    {
                        IdDeuda = deudaAuto.IdDeuda,
                        Fecha = hoy.AddMonths(-1),
                        PagoMinimo = 250m, PagoExtra = 0m,
                        Interes = 65.52m, Capital = 184.48m,
                        SaldoRestante = 8315.52m
                    });

                    // 6. Resumen anual del año actual
                    db.ResumenesAnuales.Add(new ResumenAnual
                    {
                        IdUsuario = demo.IdUsuario,
                        Anio = hoy.Year,
                        Ingresos = 18000m,
                        Ahorros = 2400m,
                        GastosFijos = 6840m,
                        GastosVariables = 4200m,
                        Deudas = 3540m
                    });

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // No queremos que un fallo del seeder tire la app entera al arrancar.
                System.Diagnostics.Debug.WriteLine("DataSeeder error: " + ex.Message);
            }
        }
    }
}
