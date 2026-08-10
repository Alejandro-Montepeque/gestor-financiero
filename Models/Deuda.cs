using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_financiero.Models
{
    [Table("Deuda")]
    public class Deuda
    {
        [Key]
        [Column("id_deuda")]
        public int IdDeuda { get; set; }

        [Required]
        [Column("id_usuario")]
        [Display(Name = "Usuario")]
        public int IdUsuario { get; set; }

        [Required]
        [StringLength(100)]
        [Column("nombre")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }

        [Required]
        [Column("saldo_actual", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Saldo Actual")]
        public decimal SaldoActual { get; set; }

        [Column("tasa_interes", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Tasa de Interés (%)")]
        public decimal? TasaInteres { get; set; }

        [Column("pago_minimo", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Pago Mínimo")]
        public decimal? PagoMinimo { get; set; }

        [StringLength(255)]
        [Column("notas")]
        [Display(Name = "Notas")]
        public string Notas { get; set; }

        // Navegación
        [ForeignKey("IdUsuario")]
        public virtual Usuario Usuario { get; set; }

        public virtual ICollection<PagoDeuda> Pagos { get; set; }
    }
}
