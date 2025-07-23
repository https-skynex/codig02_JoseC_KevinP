using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FIEEReservaEspacios.Models
{
    public enum TipoEspacio { Aula, Laboratorio, Auditorio }
    public class Espacio
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del espacio es obligatorio")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "El nombre debe tener entre 5-100 caracteres")]
        [RegularExpression(@"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\-]+$",
            ErrorMessage = "Solo letras, números, guiones y espacios")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El número de edificio es obligatorio")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Solo números enteros permitidos")]
        public string Numero_Edificio { get; set; }

        [Required(ErrorMessage = "El piso es obligatorio")]
        [Range(-10, 50, ErrorMessage = "El piso debe estar entre -10 y 50")]
        public int Piso { get; set; }

        [Required(ErrorMessage = "El código es obligatorio")]
        [RegularExpression(@"^E\d{1,2}/P\d{1,2}/E\d{3}$",
            ErrorMessage = "Formato inválido. Ejemplo: E17/P5/E025")]
        public string Codigo { get; set; }


        public enum TipoEspacio { Aula, Laboratorio, Auditorio }
        [Required(ErrorMessage = "El tipo de espacio es obligatorio")]
        [RegularExpression("^(aula|laboratorio|auditorio)$",
            ErrorMessage = "Tipo inválido. Valores permitidos: aula, laboratorio, auditorio")]

        public string Tipo { get; set; }
    }
}