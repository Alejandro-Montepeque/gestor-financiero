using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gestor_financiero.Models
{
    [Table("Categoria")]
    public class Categoria
    {
        [Key]
        [Column("id_categoria")]
        public int IdCategoria { get; set; }

        [Required]
        [StringLength(50)]
        [Column("nombre")]
        [Display(Name = "Nombre")]
        public string Nombre { get; set; }

        // Valores esperados: INGRESO, AHORRO, GASTO_FIJO, GASTO_VARIABLE, DEUDA
        [Required]
        [StringLength(20)]
        [Column("tipo")]
        [Display(Name = "Tipo")]
        public string Tipo { get; set; }

        // Navegación
        public virtual ICollection<PresupuestoDetalle> PresupuestoDetalles { get; set; }
        public virtual ICollection<Transaccion> Transacciones { get; set; }
    }

    // Constantes para evitar errores de tipeo al usar el campo Tipo
    public static class TipoCategoria
    {
        public const string Ingreso = "INGRESO";
        public const string Ahorro = "AHORRO";
        public const string GastoFijo = "GASTO_FIJO";
        public const string GastoVariable = "GASTO_VARIABLE";
        public const string Deuda = "DEUDA";
    }
}
