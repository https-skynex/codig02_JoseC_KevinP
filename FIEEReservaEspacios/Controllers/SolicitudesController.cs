using FIEEReservaEspacios.DAL;
using FIEEReservaEspacios.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace SistemaReservasEspaciosFIEE.Controllers
{
    [RoutePrefix("FIEE/reservacionespacio")]
    public class ReservaEspaciosController : ApiController
    {
        private readonly ReservaEspaciosContext db = new ReservaEspaciosContext();

        #region General Endpoints

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAllSolicitudes()
        {
            try
            {
                var solicitudes = db.Solicitudes
                    .Include(s => s.Usuario)
                    .Include(s => s.Espacio)
                    .Select(s => new {
                        s.Id,
                        s.Fecha,
                        HoraInicio = s.HoraInicio.ToString(@"hh\:mm"),
                        HoraFin = s.HoraFin.ToString(@"hh\:mm"),
                        s.Estado,
                        s.Descripcion,
                        Usuario = new { s.Usuario.Id, s.Usuario.Nombre, s.Usuario.Apellido },
                        Espacio = new { s.Espacio.Id, s.Espacio.Nombre }
                    })
                    .ToList();

                return Ok(solicitudes);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region Espacio Endpoints

        [HttpGet]
        [Route("espacios/{idEspacio}")]
        public IHttpActionResult GetSolicitudesPorEspacio(int idEspacio)
        {
            try
            {
                var espacio = db.Espacios.Find(idEspacio);
                if (espacio == null)
                    return NotFound();

                var solicitudes = db.Solicitudes
                    .Where(s => s.EspacioId == idEspacio)
                    .Include(s => s.Usuario)
                    .Select(s => new {
                        s.Id,
                        s.Fecha,
                        HoraInicio = s.HoraInicio.ToString(@"hh\:mm"),
                        HoraFin = s.HoraFin.ToString(@"hh\:mm"),
                        s.Estado,
                        s.Descripcion,
                        Usuario = new { s.Usuario.Id, s.Usuario.Nombre, s.Usuario.Apellido }
                    })
                    .ToList();

                return Ok(new
                {
                    Espacio = new { espacio.Id, espacio.Nombre },
                    Solicitudes = solicitudes
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpGet]
        [Route("espacios/{idEspacio}/{idSolicitud}")]
        public IHttpActionResult GetSolicitudPorEspacio(int idEspacio, int idSolicitud)
        {
            try
            {
                var espacio = db.Espacios.Find(idEspacio);
                if (espacio == null)
                    return NotFound();

                var solicitud = db.Solicitudes
                    .Where(s => s.Id == idSolicitud && s.EspacioId == idEspacio)
                    .Include(s => s.Usuario)
                    .Include(s => s.Espacio)
                    .Select(s => new {
                        s.Id,
                        s.Fecha,
                        HoraInicio = s.HoraInicio.ToString(@"hh\:mm"),
                        HoraFin = s.HoraFin.ToString(@"hh\:mm"),
                        s.Estado,
                        s.Descripcion,
                        Usuario = new { s.Usuario.Id, s.Usuario.Nombre, s.Usuario.Apellido },
                        Espacio = new { s.Espacio.Id, s.Espacio.Nombre, s.Espacio.Codigo }
                    })
                    .FirstOrDefault();

                if (solicitud == null)
                    return NotFound();

                return Ok(new
                {
                    Espacio = new { espacio.Id, espacio.Nombre, espacio.Codigo },
                    Solicitud = solicitud
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("espacios/{idEspacio}")]
        public IHttpActionResult CrearSolicitudEnEspacio(int idEspacio, [FromBody] SolicitudCreacionDto solicitudDto)
        {
            try
            {
                return CrearSolicitud(idEspacio, solicitudDto);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Route("espacios/{idEspacio}/{idSolicitud}")]
        [Authorize(Roles = "Administrador")]
        public IHttpActionResult ActualizarSolicitudEnEspacio(int idEspacio, int idSolicitud, [FromBody] ActualizacionEstadoDto estadoDto)
        {
            try
            {
                var solicitud = db.Solicitudes.FirstOrDefault(s => s.Id == idSolicitud && s.EspacioId == idEspacio);
                if (solicitud == null)
                    return NotFound();

                if (solicitud.Estado != "pendiente")
                    return Content(HttpStatusCode.Forbidden, "No se puede modificar una solicitud ya procesada");

                var result = ActualizarEstadoSolicitud(solicitud, estadoDto);

                if (estadoDto.Estado.ToLower() == "aprobado")
                {
                    RechazarSolicitudesConflictivas(solicitud);
                }

                return result;
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("espacios/{idEspacio}/{idSolicitud}")]
        public IHttpActionResult EliminarSolicitudEnEspacio(int idEspacio, int idSolicitud)
        {
            try
            {
                var solicitud = db.Solicitudes.FirstOrDefault(s => s.Id == idSolicitud && s.EspacioId == idEspacio);
                if (solicitud == null)
                    return NotFound();

                if (solicitud.Estado != "pendiente")
                    return Content(HttpStatusCode.Forbidden, "No se puede eliminar una solicitud ya procesada");

                db.Solicitudes.Remove(solicitud);
                db.SaveChanges();

                return Ok(new { mensaje = "Solicitud eliminada correctamente" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region Usuario Endpoints

        [HttpGet]
        [Route("usuario/{idUsuario}")]
        public IHttpActionResult GetSolicitudesPorUsuario(int idUsuario)
        {
            try
            {
                var usuario = db.Usuarios.Find(idUsuario);
                if (usuario == null)
                    return NotFound();

                var solicitudes = db.Solicitudes
                    .Where(s => s.UsuarioId == idUsuario)
                    .Include(s => s.Espacio)
                    .Select(s => new {
                        s.Id,
                        s.Fecha,
                        HoraInicio = s.HoraInicio.ToString(@"hh\:mm"),
                        HoraFin = s.HoraFin.ToString(@"hh\:mm"),
                        s.Estado,
                        s.Descripcion,
                        Espacio = new { s.Espacio.Id, s.Espacio.Nombre }
                    })
                    .ToList();

                return Ok(new
                {
                    Usuario = new { usuario.Id, usuario.Nombre, usuario.Apellido },
                    Solicitudes = solicitudes
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Route("usuario/{idUsuario}/{idSolicitud}")]
        public IHttpActionResult ActualizarSolicitudDeUsuario(int idUsuario, int idSolicitud, [FromBody] ActualizacionEstadoDto estadoDto)
        {
            try
            {
                var solicitud = db.Solicitudes.FirstOrDefault(s => s.Id == idSolicitud && s.UsuarioId == idUsuario);
                if (solicitud == null)
                    return NotFound();

                if (solicitud.Estado != "pendiente")
                    return Content(HttpStatusCode.Forbidden, "No se puede modificar una solicitud ya procesada");

                return ActualizarEstadoSolicitud(solicitud, estadoDto);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("usuario/{idUsuario}/{idSolicitud}")]
        public IHttpActionResult EliminarSolicitudDeUsuario(int idUsuario, int idSolicitud)
        {
            try
            {
                var solicitud = db.Solicitudes.FirstOrDefault(s => s.Id == idSolicitud && s.UsuarioId == idUsuario);
                if (solicitud == null)
                    return NotFound();

                if (solicitud.Estado != "pendiente")
                    return Content(HttpStatusCode.Forbidden, "No se puede eliminar una solicitud ya procesada");

                db.Solicitudes.Remove(solicitud);
                db.SaveChanges();

                return Ok(new { mensaje = "Solicitud eliminada correctamente" });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #region Helper Methods

        private void RechazarSolicitudesConflictivas(Solicitud solicitudAprobada)
        {
            var solicitudesConflictivas = db.Solicitudes
                .Where(s => s.EspacioId == solicitudAprobada.EspacioId &&
                           s.Fecha == solicitudAprobada.Fecha &&
                           s.Id != solicitudAprobada.Id &&
                           s.Estado == "pendiente" &&
                           s.HoraInicio < solicitudAprobada.HoraFin &&
                           s.HoraFin > solicitudAprobada.HoraInicio)
                .ToList();

            foreach (var solicitud in solicitudesConflictivas)
            {
                solicitud.Estado = "rechazado";
            }

            db.SaveChanges();
        }

        private IHttpActionResult ActualizarEstadoSolicitud(Solicitud solicitud, ActualizacionEstadoDto estadoDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!new[] { "aprobado", "rechazado" }.Contains(estadoDto.Estado.ToLower()))
                return BadRequest("Estado inválido. Use 'aprobado' o 'rechazado'");

            solicitud.Estado = estadoDto.Estado.ToLower();
            db.SaveChanges();

            return Ok(new
            {
                mensaje = "Estado actualizado correctamente",
                solicitud.Id,
                solicitud.Estado
            });
        }

        private IHttpActionResult CrearSolicitud(int idEspacio, SolicitudCreacionDto solicitudDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var espacio = db.Espacios.Find(idEspacio);
            if (espacio == null)
                return NotFound();

            var usuario = db.Usuarios.Find(solicitudDto.UsuarioId);
            if (usuario == null)
                return BadRequest("Usuario no encontrado");

            if (solicitudDto.HoraFin <= solicitudDto.HoraInicio)
                return BadRequest("La hora de fin debe ser posterior a la hora de inicio");

            if (ExisteConflictoHorario(idEspacio, solicitudDto.Fecha, solicitudDto.HoraInicio, solicitudDto.HoraFin))
                return Content(HttpStatusCode.Conflict, "El espacio ya está reservado en ese horario");

            var solicitud = new Solicitud
            {
                UsuarioId = solicitudDto.UsuarioId,
                EspacioId = idEspacio,
                Fecha = solicitudDto.Fecha,
                HoraInicio = solicitudDto.HoraInicio,
                HoraFin = solicitudDto.HoraFin,
                Descripcion = solicitudDto.Descripcion,
                Estado = "pendiente"
            };

            var validationContext = new ValidationContext(solicitud);
            var validationResults = new List<ValidationResult>();
            if (!Validator.TryValidateObject(solicitud, validationContext, validationResults, true))
            {
                return BadRequest(string.Join("; ", validationResults.Select(r => r.ErrorMessage)));
            }

            db.Solicitudes.Add(solicitud);
            db.SaveChanges();

            return Created(
                new Uri($"{Request.RequestUri}/{solicitud.Id}"),
                new
                {
                    solicitud.Id,
                    solicitud.Fecha,
                    HoraInicio = solicitud.HoraInicio.ToString(@"hh\:mm"),
                    HoraFin = solicitud.HoraFin.ToString(@"hh\:mm"),
                    solicitud.Estado,
                    solicitud.Descripcion,
                    Usuario = new { usuario.Nombre, usuario.Apellido },
                    Espacio = new { espacio.Nombre, espacio.Codigo }
                });
        }

        private bool ExisteConflictoHorario(int espacioId, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin)
        {
            return db.Solicitudes
                .Any(s => s.EspacioId == espacioId &&
                           DbFunctions.TruncateTime(s.Fecha) == DbFunctions.TruncateTime(fecha) &&
                           s.Estado != "rechazado" &&
                           s.HoraInicio < horaFin &&
                           s.HoraFin > horaInicio);
        }

        #endregion
    }

    // DTOs
    public class SolicitudCreacionDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int UsuarioId { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public TimeSpan HoraInicio { get; set; }

        [Required]
        public TimeSpan HoraFin { get; set; }

        [Required(ErrorMessage = "La descripción es obligatoria")]
        [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres")]
        public string Descripcion { get; set; }
    }

    public class ActualizacionEstadoDto
    {
        [Required]
        [RegularExpression("^(aprobado|rechazado)$", ErrorMessage = "Estado debe ser 'aprobado' o 'rechazado'")]
        public string Estado { get; set; }
    }
}