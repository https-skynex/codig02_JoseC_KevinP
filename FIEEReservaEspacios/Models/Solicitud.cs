using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace FIEEReservaEspacios.Models
{
    public class Solicitud
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "El ID de usuario es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "ID de usuario inválido")]
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El ID de espacio es obligatorio")]
        [Range(1, int.MaxValue, ErrorMessage = "ID de espacio inválido")]
        public int EspacioId { get; set; }

        // Propiedades de navegación
        public virtual Usuario Usuario { get; set; }
        public virtual Espacio Espacio { get; set; }

        [Required(ErrorMessage = "La fecha es obligatoria")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [CustomValidation(typeof(Solicitud), "ValidarFechaFutura")]
        public DateTime Fecha { get; set; }

        [Required(ErrorMessage = "Hora de inicio es obligatoria")]
        [DataType(DataType.Time)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = @"{0:hh\:mm}")]
        public TimeSpan HoraInicio { get; set; }

        [Required(ErrorMessage = "Hora de fin es obligatoria")]
        [DataType(DataType.Time)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = @"{0:hh\:mm}")]
        [CustomValidation(typeof(Solicitud), "ValidarHoraFin")]
        public TimeSpan HoraFin { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
        [DataType(DataType.MultilineText)]
        public string Descripcion { get; set; }

        [Required]
        [RegularExpression("^(pendiente|aprobado|rechazado)$",
            ErrorMessage = "Estado inválido")]
        public string Estado { get; set; } = "pendiente";

        // Validación personalizada para fecha futura
        public static ValidationResult ValidarFechaFutura(DateTime fecha)
        {
            return fecha.Date > DateTime.Today
                ? ValidationResult.Success
                : new ValidationResult("La fecha debe ser al menos un día después de hoy");
        }

        // Validación personalizada para hora fin > hora inicio
        public static ValidationResult ValidarHoraFin(TimeSpan horaFin, ValidationContext context)
        {
            var solicitud = (Solicitud)context.ObjectInstance;

            if (horaFin <= solicitud.HoraInicio)
            {
                return new ValidationResult("La hora de fin debe ser posterior a la hora de inicio");
            }

            // Validar duración máxima (ej. 8 horas)
            if ((horaFin - solicitud.HoraInicio).TotalHours > 8)
            {
                return new ValidationResult("La reserva no puede exceder 8 horas");
            }

            return ValidationResult.Success;
        }
    }
}