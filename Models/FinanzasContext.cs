using System.Data.Entity;

namespace gestor_financiero.Models
{
    // IMPORTANTE: requiere instalar el paquete NuGet "EntityFramework" (versión 6.x)
    //
    // En la Consola del Administrador de Paquetes ejecutar:
    //     Install-Package EntityFramework -Version 6.4.4
    //
    // Y agregar en Web.config la cadena de conexión "FinanzasContext", por ejemplo:
    //
    // <connectionStrings>
    //   <add name="FinanzasContext"
    //        connectionString="Data Source=.;Initial Catalog=FinanzasPersonales;Integrated Security=True"
    //        providerName="System.Data.SqlClient" />
    // </connectionStrings>
    public class FinanzasContext : DbContext
    {
        public FinanzasContext() : base("name=FinanzasContext")
        {
            // Como la BD ya existe, evitamos que EF intente crearla o migrarla.
            Database.SetInitializer<FinanzasContext>(null);
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Presupuesto> Presupuestos { get; set; }
        public DbSet<PresupuestoDetalle> PresupuestoDetalles { get; set; }
        public DbSet<Transaccion> Transacciones { get; set; }
        public DbSet<Deuda> Deudas { get; set; }
        public DbSet<PagoDeuda> PagosDeuda { get; set; }
        public DbSet<ResumenAnual> ResumenesAnuales { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Precisión decimal explícita para todas las columnas monetarias
            modelBuilder.Entity<PresupuestoDetalle>().Property(p => p.Estimado).HasPrecision(12, 2);
            modelBuilder.Entity<PresupuestoDetalle>().Property(p => p.Real).HasPrecision(12, 2);
            modelBuilder.Entity<Transaccion>().Property(t => t.Monto).HasPrecision(12, 2);
            modelBuilder.Entity<Deuda>().Property(d => d.SaldoActual).HasPrecision(12, 2);
            modelBuilder.Entity<Deuda>().Property(d => d.TasaInteres).HasPrecision(5, 2);
            modelBuilder.Entity<Deuda>().Property(d => d.PagoMinimo).HasPrecision(12, 2);
            modelBuilder.Entity<PagoDeuda>().Property(p => p.PagoMinimo).HasPrecision(12, 2);
            modelBuilder.Entity<PagoDeuda>().Property(p => p.PagoExtra).HasPrecision(12, 2);
            modelBuilder.Entity<PagoDeuda>().Property(p => p.Interes).HasPrecision(12, 2);
            modelBuilder.Entity<PagoDeuda>().Property(p => p.Capital).HasPrecision(12, 2);
            modelBuilder.Entity<PagoDeuda>().Property(p => p.SaldoRestante).HasPrecision(12, 2);
            modelBuilder.Entity<ResumenAnual>().Property(r => r.Ingresos).HasPrecision(12, 2);
            modelBuilder.Entity<ResumenAnual>().Property(r => r.Ahorros).HasPrecision(12, 2);
            modelBuilder.Entity<ResumenAnual>().Property(r => r.GastosFijos).HasPrecision(12, 2);
            modelBuilder.Entity<ResumenAnual>().Property(r => r.GastosVariables).HasPrecision(12, 2);
            modelBuilder.Entity<ResumenAnual>().Property(r => r.Deudas).HasPrecision(12, 2);

            base.OnModelCreating(modelBuilder);
        }
    }
}
