using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_financiero.Models
{
    [Table("ResumenAnual")]
    public class ResumenAnual
    {
        [Key]
        [Column("id_resumen")]
        public int IdResumen { get; set; }

        [Required]
        [Column("id_usuario")]
        [Display(Name = "Usuario")]
        public int IdUsuario { get; set; }

        [Required]
        [Range(2000, 2100)]
        [Column("anio")]
        [Display(Name = "Año")]
        public int Anio { get; set; }

        [Column("ingresos", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Ingresos")]
        public decimal? Ingresos { get; set; }

        [Column("ahorros", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Ahorros")]
        public decimal? Ahorros { get; set; }

        [Column("gastos_fijos", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Gastos Fijos")]
        public decimal? GastosFijos { get; set; }

        [Column("gastos_variables", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Gastos Variables")]
        public decimal? GastosVariables { get; set; }

        [Column("deudas", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Deudas")]
        public decimal? Deudas { get; set; }

        // Navegación
        [ForeignKey("IdUsuario")]
        public virtual Usuario Usuario { get; set; }

        // Helper: balance neto del año
        [NotMapped]
        [Display(Name = "Balance Neto")]
        public decimal BalanceNeto =>
            (Ingresos ?? 0) - (GastosFijos ?? 0) - (GastosVariables ?? 0) - (Deudas ?? 0);
    }
}
