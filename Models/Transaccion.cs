using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_financiero.Models
{
    [Table("Transaccion")]
    public class Transaccion
    {
        [Key]
        [Column("id_transaccion")]
        public int IdTransaccion { get; set; }

        [Required]
        [Column("id_usuario")]
        [Display(Name = "Usuario")]
        public int IdUsuario { get; set; }

        [Required]
        [Column("id_categoria")]
        [Display(Name = "Categoría")]
        public int IdCategoria { get; set; }

        [Required]
        [Column("monto", TypeName = "decimal")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Display(Name = "Monto")]
        public decimal Monto { get; set; }

        [Required]
        [Column("fecha", TypeName = "date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Fecha")]
        public DateTime Fecha { get; set; }

        [StringLength(255)]
        [Column("notas")]
        [Display(Name = "Notas")]
        public string Notas { get; set; }

        // Navegación
        [ForeignKey("IdUsuario")]
        public virtual Usuario Usuario { get; set; }

        [ForeignKey("IdCategoria")]
        public virtual Categoria Categoria { get; set; }
    }
}
