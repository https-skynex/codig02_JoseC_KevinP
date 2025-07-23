using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FIEEReservaEspacios.Models
{
    public class Usuario
    {
        [Key] // Especifica que es la clave primaria
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-incremental
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2-50 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Solo letras permitidas")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "El apellido debe tener entre 2-50 caracteres")]
        [RegularExpression(@"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$", ErrorMessage = "Solo letras permitidas")]
        public string Apellido { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "Formato de correo inválido")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(fiec\.)?espol\.edu\.ec$",
            ErrorMessage = "Solo correos institucionales ESPOL permitidos")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Mínimo 8 caracteres")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
            ErrorMessage = "Debe incluir mayúsculas, minúsculas, números y símbolos")]
        public string Contrasena { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        [RegularExpression("^(Profesor|Administrador|Coordinador)$",
            ErrorMessage = "Rol inválido. Valores permitidos: Profesor, Administrador, Coordinador")]
        public string Rol { get; set; }
    }
}