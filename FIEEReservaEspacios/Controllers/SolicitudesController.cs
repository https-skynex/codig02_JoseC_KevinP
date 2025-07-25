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

        #region Helper Methods for Authentication

        private Usuario AuthenticateUser(CredencialesDto credenciales)
        {
            if (credenciales == null || string.IsNullOrEmpty(credenciales.Correo) || string.IsNullOrEmpty(credenciales.Password))
                return null;

            var usuario = db.Usuarios.FirstOrDefault(u => u.Correo == credenciales.Correo && u.Contrasena == credenciales.Password);
            return usuario;
        }

        private IHttpActionResult CheckAdminAuthorization(CredencialesDto credenciales)
        {
            var usuario = AuthenticateUser(credenciales);
            if (usuario == null)
                return Unauthorized();

            if (usuario.Rol != "Administrador")
                return Content(HttpStatusCode.Forbidden, "Usuario no autorizado. Se requiere rol de Administrador");
            return null;
        }

        private IHttpActionResult CheckProfessorAdminCoordAuthorization(CredencialesDto credenciales)
        {
            var usuario = AuthenticateUser(credenciales);
            if (usuario == null)
                return Content(HttpStatusCode.NotFound, "Usuario no encontrado");

            var rolesPermitidos = new[] { "Profesor", "Administrador", "Coordinador" };
            if (!rolesPermitidos.Contains(usuario.Rol))
                return Content(HttpStatusCode.Forbidden, "Usuario no autorizado. Se requiere rol de Profesor, Administrador o Coordinador");

            return null;
        }

        #endregion

        #region General Endpoints

        [HttpPost]
        [Route("")]
        public IHttpActionResult GetAllSolicitudes([FromBody] CredencialesDto credenciales)
        {
            try
            {
                var authResult = CheckAdminAuthorization(credenciales);
                if (authResult != null)
                    return authResult;

                var solicitudes = db.Solicitudes
    .Include(s => s.Usuario)
    .Include(s => s.Espacio)
    .Select(s => new {
        s.Id,
        s.Fecha,
        s.HoraInicio,
        s.HoraFin,
        s.Estado,
        s.Descripcion,
        Usuario = new { s.Usuario.Id, s.Usuario.Nombre, s.Usuario.Apellido },
        Espacio = new { s.Espacio.Id, s.Espacio.Nombre }
    })
    .AsEnumerable()
    .Select(s => new {
        s.Id,
        s.Fecha,
        HoraInicio = s.HoraInicio.ToString(@"hh\:mm"),
        HoraFin = s.HoraFin.ToString(@"hh\:mm"),
        s.Estado,
        s.Descripcion,
        s.Usuario,
        s.Espacio
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

        [HttpPost]
        [Route("espacios/{idEspacio}")]
        public IHttpActionResult GetSolicitudesPorEspacio(int idEspacio, [FromBody] CredencialesDto credenciales)
        {
            try
            {
                var authResult = CheckAdminAuthorization(credenciales);
                if (authResult != null)
                    return authResult;

                var espacio = db.Espacios.Find(idEspacio);
                if (espacio == null)
                    return NotFound();

                var solicitudes = db.Solicitudes
                .Where(s => s.EspacioId == idEspacio)
                .Include(s => s.Usuario)
                .Select(s => new {
                    s.Id,
                    s.Fecha,
                    s.HoraInicio,
                    s.HoraFin,
                    s.Estado,
                    s.Descripcion,
                    Usuario = new { s.Usuario.Id, s.Usuario.Nombre, s.Usuario.Apellido }
                })
                .AsEnumerable()
                .Select(s => new {
                    s.Id,
                    s.Fecha,
                    HoraInicio = s.HoraInicio.ToString(@"hh\:mm"),
                    HoraFin = s.HoraFin.ToString(@"hh\:mm"),
                    s.Estado,
                    s.Descripcion,
                    s.Usuario
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

        [HttpPost]
        [Route("espacios/{idEspacio}/{idSolicitud}")]
        public IHttpActionResult GetSolicitudPorEspacio(int idEspacio, int idSolicitud, [FromBody] CredencialesDto credenciales)
        {
            try
            {
                var authResult = CheckAdminAuthorization(credenciales);
                if (authResult != null)
                    return authResult;

                var espacio = db.Espacios.Find(idEspacio);
                if (espacio == null)
                    return NotFound();

                // Primero obtener los datos sin el formato de hora
                var solicitud = db.Solicitudes
                    .Where(s => s.Id == idSolicitud && s.EspacioId == idEspacio)
                    .Include(s => s.Usuario)
                    .Include(s => s.Espacio)
                    .Select(s => new {
                        s.Id,
                        s.Fecha,
                        s.HoraInicio,
                        s.HoraFin,
                        s.Estado,
                        s.Descripcion,
                        Usuario = new { s.Usuario.Id, s.Usuario.Nombre, s.Usuario.Apellido },
                        Espacio = new { s.Espacio.Id, s.Espacio.Nombre, s.Espacio.Codigo }
                    })
                    .AsEnumerable() // Esto hace que lo siguiente se ejecute en memoria
                    .Select(s => new {
                        s.Id,
                        s.Fecha,
                        HoraInicio = s.HoraInicio.ToString(@"hh\:mm"),
                        HoraFin = s.HoraFin.ToString(@"hh\:mm"),
                        s.Estado,
                        s.Descripcion,
                        s.Usuario,
                        s.Espacio
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
        [Route("espacios/crear/{idEspacio}")]
        public IHttpActionResult CrearSolicitudEnEspacio(int idEspacio, [FromBody] SolicitudConCredencialesDto solicitudConCredenciales)
        {
            try
            {
                var authResult = CheckProfessorAdminCoordAuthorization(solicitudConCredenciales.Credenciales);
                if (authResult != null)
                    return authResult;

                // Verificar que el usuario que hace la solicitud coincide con el usuario autenticado
                var usuarioAutenticado = AuthenticateUser(solicitudConCredenciales.Credenciales);
                if (usuarioAutenticado.Id != solicitudConCredenciales.Solicitud.UsuarioId)
                    return Content(HttpStatusCode.Forbidden, "No puedes crear solicitudes para otros usuarios");

                return CrearSolicitud(idEspacio, solicitudConCredenciales.Solicitud);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPut]
        [Route("espacios/{idEspacio}/{idSolicitud}")]
        public IHttpActionResult ActualizarSolicitudEnEspacio(int idEspacio, int idSolicitud, [FromBody] ActualizacionEstadoConCredencialesDto estadoConCredenciales)
        {
            try
            {
                var authResult = CheckAdminAuthorization(estadoConCredenciales.Credenciales);
                if (authResult != null)
                    return authResult;

                var solicitud = db.Solicitudes.FirstOrDefault(s => s.Id == idSolicitud && s.EspacioId == idEspacio);
                if (solicitud == null)
                    return NotFound();

                if (solicitud.Estado != "pendiente")
                    return Content(HttpStatusCode.Forbidden, "No se puede modificar una solicitud ya procesada");

                var result = ActualizarEstadoSolicitud(solicitud, estadoConCredenciales.EstadoDto);

                if (estadoConCredenciales.EstadoDto.Estado.ToLower() == "aprobado")
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

        [HttpPost]
        [Route("espacios/eliminar/{idEspacio}/{idSolicitud}")]
        public IHttpActionResult EliminarSolicitudEnEspacio(int idEspacio, int idSolicitud, [FromBody] CredencialesDto credenciales)
        {
            try
            {
                var authResult = CheckProfessorAdminCoordAuthorization(credenciales);
                if (authResult != null)
                    return authResult;

                var solicitud = db.Solicitudes.FirstOrDefault(s => s.Id == idSolicitud && s.EspacioId == idEspacio);
                if (solicitud == null)
                    return NotFound();

                // Verificar que el usuario autenticado es el dueño de la solicitud o es admin/coord
                var usuarioAutenticado = AuthenticateUser(credenciales);
                if (usuarioAutenticado.Id != solicitud.UsuarioId && usuarioAutenticado.Rol != "Administrador" && usuarioAutenticado.Rol != "Coordinador")
                    return Content(HttpStatusCode.Forbidden, "No tienes permiso para eliminar esta solicitud");

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

        [HttpPost]
        [Route("usuario/{idUsuario}")]
        public IHttpActionResult GetSolicitudesPorUsuario(int idUsuario, [FromBody] CredencialesDto credenciales)
        {
            try
            {
                var authResult = CheckProfessorAdminCoordAuthorization(credenciales);
                if (authResult != null)
                    return authResult;

                // Verificar que el usuario autenticado está accediendo a sus propias solicitudes o es admin/coord
                var usuarioAutenticado = AuthenticateUser(credenciales);
                if (usuarioAutenticado.Id != idUsuario && usuarioAutenticado.Rol != "Administrador" && usuarioAutenticado.Rol != "Coordinador")
                    return Content(HttpStatusCode.Forbidden, "No tienes permiso para ver estas solicitudes");

                var usuario = db.Usuarios.Find(idUsuario);
                if (usuario == null)
                    return NotFound();

                var solicitudes = db.Solicitudes
                    .Where(s => s.UsuarioId == idUsuario)
                    .Include(s => s.Espacio)
                    .Select(s => new {
                        s.Id,
                        s.Fecha,
                        s.HoraInicio,
                        s.HoraFin,
                        s.Estado,
                        s.Descripcion,
                        Espacio = new { s.Espacio.Id, s.Espacio.Nombre }
                    })
                    .AsEnumerable()
                    .Select(s => new {
                        s.Id,
                        s.Fecha,
                        HoraInicio = s.HoraInicio.ToString(@"hh\:mm"),
                        HoraFin = s.HoraFin.ToString(@"hh\:mm"),
                        s.Estado,
                        s.Descripcion,
                        s.Espacio
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
        public IHttpActionResult ActualizarSolicitudDeUsuario(int idUsuario, int idSolicitud, [FromBody] ActualizacionSolicitudConCredencialesDto solicitudConCredenciales)
        {
            try
            {
                var authResult = CheckProfessorAdminCoordAuthorization(solicitudConCredenciales.Credenciales);
                if (authResult != null)
                    return authResult;

                // Verificar que el usuario autenticado es el dueño de la solicitud
                var usuarioAutenticado = AuthenticateUser(solicitudConCredenciales.Credenciales);
                if (usuarioAutenticado.Id != idUsuario)
                    return Content(HttpStatusCode.Forbidden, "No puedes modificar solicitudes de otros usuarios");

                var solicitud = db.Solicitudes.FirstOrDefault(s => s.Id == idSolicitud && s.UsuarioId == idUsuario);
                if (solicitud == null)
                    return NotFound();

                if (solicitud.Estado != "pendiente")
                    return Content(HttpStatusCode.Forbidden, "No se puede modificar una solicitud ya procesada");

                return ActualizarEstadoSolicitud(solicitud, solicitudConCredenciales.EstadoDto);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("usuario/eliminar/{idUsuario}/{idSolicitud}")]
        public IHttpActionResult EliminarSolicitudDeUsuario(int idUsuario, int idSolicitud, [FromBody] CredencialesDto credenciales)
        {
            try
            {
                var authResult = CheckProfessorAdminCoordAuthorization(credenciales);
                if (authResult != null)
                    return authResult;

                // Verificar que el usuario autenticado es el dueño de la solicitud
                var usuarioAutenticado = AuthenticateUser(credenciales);
                if (usuarioAutenticado.Id != idUsuario)
                    return Content(HttpStatusCode.Forbidden, "No puedes eliminar solicitudes de otros usuarios");

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
                    Usuario = new { usuario.Nombre, usuario.Apellido, usuario.Correo },
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
    public class CredencialesDto
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido")]
        public string Correo { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        public string Password { get; set; }
    }

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

    public class SolicitudConCredencialesDto
    {
        [Required]
        public CredencialesDto Credenciales { get; set; }

        [Required]
        public SolicitudCreacionDto Solicitud { get; set; }
    }

    public class ActualizacionEstadoDto
    {
        [Required]
        [RegularExpression("^(aprobado|rechazado)$", ErrorMessage = "Estado debe ser 'aprobado' o 'rechazado'")]
        public string Estado { get; set; }
    }

    public class ActualizacionEstadoConCredencialesDto
    {
        [Required]
        public CredencialesDto Credenciales { get; set; }

        [Required]
        public ActualizacionEstadoDto EstadoDto { get; set; }
    }

    public class ActualizacionSolicitudConCredencialesDto
    {
        [Required]
        public CredencialesDto Credenciales { get; set; }

        [Required]
        public ActualizacionEstadoDto EstadoDto { get; set; }
    }
}