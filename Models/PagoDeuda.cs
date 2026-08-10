using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_financiero.Models
{
    [Table("PagoDeuda")]
    public class PagoDeuda
    {
        [Key]
        [Column("id_pago")]
        public int IdPago { get; set; }

        [Required]
        [Column("id_deuda")]
        [Display(Name = "Deuda")]
        public int IdDeuda { get; set; }

        [Required]
        [Column("fecha", TypeName = "date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; }

        [Column("pago_minimo", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Pago Mínimo")]
        public decimal PagoMinimo { get; set; }

        [Column("pago_extra", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Pago Extra")]
        public decimal PagoExtra { get; set; }

        [Column("interes", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Interés")]
        public decimal Interes { get; set; }

        [Column("capital", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Capital")]
        public decimal Capital { get; set; }

        [Column("saldo_restante", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Saldo Restante")]
        public decimal? SaldoRestante { get; set; }

        // Navegación
        [ForeignKey("IdDeuda")]
        public virtual Deuda Deuda { get; set; }

        // Helper: total pagado = mínimo + extra
        [NotMapped]
        [Display(Name = "Total Pagado")]
        public decimal TotalPagado => PagoMinimo + PagoExtra;
    }
}
