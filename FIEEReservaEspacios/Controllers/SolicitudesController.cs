using FIEEReservaEspacios.DAL;
using FIEEReservaEspacios.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace SistemaReservasEspaciosFIEE.Controllers
{
    [RoutePrefix("FIEE/reservacionespacio")]
    public class ReservaEspaciosController : ApiController
    {
        private readonly ReservaEspaciosContext db = new ReservaEspaciosContext();

        #region Helper Methods for Authentication

        /// <summary>
        /// Autentica un usuario basado en sus credenciales
        /// </summary>
        private Usuario AuthenticateUser(CredencialesDto credenciales)
        {
            if (credenciales == null || string.IsNullOrEmpty(credenciales.Correo) || string.IsNullOrEmpty(credenciales.Password))
                return null;

            var usuario = db.Usuarios.FirstOrDefault(u => u.Correo == credenciales.Correo && u.Contrasena == credenciales.Password);
            return usuario;
        }

        /// <summary>
        /// Verifica si el usuario es administrador
        /// </summary>
        private IHttpActionResult CheckAdminAuthorization(CredencialesDto credenciales)
        {
            var usuario = AuthenticateUser(credenciales);
            if (usuario == null)
                return Unauthorized();

            if (usuario.Rol != "Administrador")
                return Content(HttpStatusCode.Forbidden, "Usuario no autorizado. Se requiere rol de Administrador");
            return null;
        }

        /// <summary>
        /// Verifica si el usuario es profesor, admin o coordinador
        /// </summary>
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

        /// <summary>
        /// Verifica si el usuario es admin o coordinador
        /// </summary>
        private IHttpActionResult CheckAdminOrCoordinatorAuthorization(CredencialesDto credenciales)
        {
            var usuario = AuthenticateUser(credenciales);
            if (usuario == null)
                return Unauthorized();

            if (usuario.Rol != "Administrador" && usuario.Rol != "Coordinador")
                return Content(HttpStatusCode.Forbidden, "Usuario no autorizado. Se requiere rol de Administrador o Coordinador");

            return null;
        }

        #endregion

        #region General Endpoints

        /// <summary>
        /// Obtiene todas las solicitudes de reserva (solo para administradores)
        /// </summary>
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

        /// <summary>
        /// Obtiene los espacios disponibles para un horario específico
        /// </summary>
        [HttpPost]
        [Route("disponibles")]
        [ResponseType(typeof(List<EspacioDisponibleDto>))]
        public IHttpActionResult GetEspaciosDisponibles([FromBody] ConsultaDisponibilidadConCredencialesDto consulta)
        {
            try
            {
                // Autenticación básica
                var authResult = CheckProfessorAdminCoordAuthorization(consulta.Credenciales);
                if (authResult != null)
                    return authResult;

                // Validar el modelo
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validar que la hora fin sea mayor a la hora inicio
                if (consulta.Consulta.HoraFin <= consulta.Consulta.HoraInicio)
                    return BadRequest("La hora de fin debe ser posterior a la hora de inicio");

                // Obtener todos los espacios
                var espacios = db.Espacios.ToList();

                // Obtener las reservas aprobadas que coincidan con la fecha
                var reservas = db.Solicitudes
                    .Where(s => s.Fecha == consulta.Consulta.Fecha &&
                               s.Estado == "aprobado")
                    .ToList();

                // Filtrar espacios disponibles
                var espaciosDisponibles = new List<EspacioDisponibleDto>();

                foreach (var espacio in espacios)
                {
                    // Verificar si hay reservas que se solapen con el horario solicitado
                    var tieneConflictos = reservas
                        .Where(r => r.EspacioId == espacio.Id)
                        .Any(r => r.HoraInicio < consulta.Consulta.HoraFin &&
                                 r.HoraFin > consulta.Consulta.HoraInicio);

                    if (!tieneConflictos)
                    {
                        espaciosDisponibles.Add(new EspacioDisponibleDto
                        {
                            Id = espacio.Id,
                            Nombre = espacio.Nombre,
                            Codigo = espacio.Codigo,
                            Tipo = espacio.Tipo,
                            Disponible = true
                        });
                    }
                }

                if (!espaciosDisponibles.Any())
                {
                    return Content(HttpStatusCode.OK, new
                    {
                        Mensaje = "No hay espacios disponibles para el horario solicitado",
                        EspaciosDisponibles = espaciosDisponibles
                    });
                }

                return Ok(new
                {
                    Mensaje = $"{espaciosDisponibles.Count} espacios disponibles encontrados",
                    EspaciosDisponibles = espaciosDisponibles
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Obtiene todas las solicitudes para un espacio específico
        /// </summary>
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

        /// <summary>
        /// Obtiene una solicitud específica para un espacio
        /// </summary>
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

        /// <summary>
        /// Crea una nueva solicitud de reserva para un espacio
        /// </summary>
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

        /// <summary>
        /// Actualiza el estado de una solicitud para un espacio (solo admin)
        /// </summary>
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

        /// <summary>
        /// Elimina una solicitud para un espacio
        /// </summary>
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

        /// <summary>
        /// Obtiene todas las solicitudes de un usuario específico
        /// </summary>
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

        /// <summary>
        /// Actualiza una solicitud específica de un usuario
        /// </summary>
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

        /// <summary>
        /// Elimina una solicitud específica de un usuario
        /// </summary>
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

        #region ExamenFinal
        /// <summary>
        /// Consulta reservas agrupadas por periodo (día, semana o mes)
        /// </summary>
        [HttpPost]
        [Route("consulta/espacio/{idEspacio}")]
        public IHttpActionResult ConsultarReservasPorPeriodo(
            int idEspacio,
            [FromBody] ConsultaPeriodoDto consulta)
        {
            try
            {
                // Autenticación básica
                var usuario = AuthenticateUser(consulta.Credenciales);
                if (usuario == null)
                    return Unauthorized();

                var espacio = db.Espacios.Find(idEspacio);
                if (espacio == null)
                    return NotFound();

                var solicitudes = db.Solicitudes
                    .Where(s => s.EspacioId == idEspacio &&
                                s.Fecha >= consulta.FechaInicio &&
                                s.Fecha <= consulta.FechaFin)
                    .Include(s => s.Usuario)
                    .ToList();

                // Agrupación por periodo
                IEnumerable<object> agrupado;
                switch (consulta.TipoPeriodo.ToLower())
                {
                    case "dia":
                        agrupado = solicitudes
                            .GroupBy(s => s.Fecha.Date)
                            .OrderBy(g => g.Key)
                            .Select(g => new {
                                Fecha = g.Key,
                                Reservas = g.Select(s => new {
                                    s.Id,
                                    s.HoraInicio,
                                    s.HoraFin,
                                    s.Estado,
                                    Usuario = new { s.Usuario.Id, s.Usuario.Nombre, s.Usuario.Apellido }
                                }).ToList()
                            });
                        break;
                    case "semana":
                        agrupado = solicitudes
                            .GroupBy(s => System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                                s.Fecha, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                            .OrderBy(g => g.Key)
                            .Select(g => new {
                                Semana = g.Key,
                                Reservas = g.Select(s => new {
                                    s.Id,
                                    s.Fecha,
                                    s.HoraInicio,
                                    s.HoraFin,
                                    s.Estado,
                                    Usuario = new { s.Usuario.Id, s.Usuario.Nombre, s.Usuario.Apellido }
                                }).ToList()
                            });
                        break;
                    case "mes":
                        agrupado = solicitudes
                            .GroupBy(s => new { s.Fecha.Year, s.Fecha.Month })
                            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                            .Select(g => new {
                                Mes = $"{g.Key.Month}/{g.Key.Year}",
                                Reservas = g.Select(s => new {
                                    s.Id,
                                    s.Fecha,
                                    s.HoraInicio,
                                    s.HoraFin,
                                    s.Estado,
                                    Usuario = new { s.Usuario.Id, s.Usuario.Nombre, s.Usuario.Apellido }
                                }).ToList()
                            });
                        break;
                    default:
                        return BadRequest("TipoPeriodo inválido. Use 'dia', 'semana' o 'mes'.");
                }

                return Ok(new
                {
                    Espacio = new { espacio.Id, espacio.Nombre },
                    Periodo = consulta.TipoPeriodo,
                    ReservasAgrupadas = agrupado
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Consulta avanzada con filtros por usuario, tipo de espacio y estado
        /// </summary>
        [HttpPost]
        [Route("consulta/avanzada")]
        public IHttpActionResult ConsultaAvanzada([FromBody] ConsultaAvanzadaDto consulta)
        {
            try
            {
                var authResult = CheckAdminAuthorization(consulta.Credenciales);
                if (authResult != null)
                    return authResult;

                var solicitudes = db.Solicitudes
                    .Include(s => s.Usuario)
                    .Include(s => s.Espacio)
                    .AsQueryable();

                if (consulta.UsuarioId.HasValue)
                    solicitudes = solicitudes.Where(s => s.UsuarioId == consulta.UsuarioId.Value);

                if (!string.IsNullOrEmpty(consulta.TipoEspacio))
                    solicitudes = solicitudes.Where(s => s.Espacio.Tipo == consulta.TipoEspacio);

                if (!string.IsNullOrEmpty(consulta.Estado))
                    solicitudes = solicitudes.Where(s => s.Estado == consulta.Estado);

                var resultado = solicitudes
                    .OrderBy(s => s.Fecha)
                    .ThenBy(s => s.HoraInicio)
                    .Select(s => new
                    {
                        s.Id,
                        s.Fecha,
                        s.HoraInicio, // Mantenemos como TimeSpan
                        s.HoraFin,    // Mantenemos como TimeSpan
                        s.Estado,
                        s.Descripcion,
                        Usuario = new { s.Usuario.Id, s.Usuario.Nombre, s.Usuario.Apellido },
                        Espacio = new { s.Espacio.Id, s.Espacio.Nombre, s.Espacio.Tipo }
                    })
                    .ToList();

                // Formateamos las horas después de materializar la consulta
                var resultadoFormateado = resultado.Select(s => new
                {
                    s.Id,
                    Fecha = s.Fecha.ToString("dd/MM/yyyy"),
                    HoraInicio = s.HoraInicio.ToString(@"hh\:mm"),
                    HoraFin = s.HoraFin.ToString(@"hh\:mm"),
                    s.Estado,
                    s.Descripcion,
                    s.Usuario,
                    s.Espacio
                }).ToList();

                return Ok(resultadoFormateado);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Exporta los resultados de una consulta a PDF
        /// </summary>
        [HttpPost]
        [Route("consulta/exportar")]
        public IHttpActionResult ExportarConsulta([FromBody] ExportarConsultaDto consulta)
        {
            try
            {
                var authResult = CheckAdminOrCoordinatorAuthorization(consulta.Credenciales);
                if (authResult != null)
                    return authResult;

                var solicitudes = db.Solicitudes
                    .Include(s => s.Usuario)
                    .Include(s => s.Espacio)
                    .AsQueryable();

                if (consulta.UsuarioId.HasValue)
                    solicitudes = solicitudes.Where(s => s.UsuarioId == consulta.UsuarioId.Value);

                if (!string.IsNullOrEmpty(consulta.TipoEspacio))
                    solicitudes = solicitudes.Where(s => s.Espacio.Tipo == consulta.TipoEspacio);

                if (!string.IsNullOrEmpty(consulta.Estado))
                    solicitudes = solicitudes.Where(s => s.Estado == consulta.Estado);

                var resultado = solicitudes
                .OrderBy(s => s.Fecha)
                .ToList()
                .Select(s => new
                {
                    s.Id,
                    s.Fecha,
                    s.HoraInicio,  // Mantén como TimeSpan
                    s.HoraFin,     // Mantén como TimeSpan
                    s.Estado,
                    s.Descripcion,
                    Usuario = new { s.Usuario.Id, s.Usuario.Nombre, s.Usuario.Apellido },
                    Espacio = new { s.Espacio.Id, s.Espacio.Nombre, s.Espacio.Tipo }
                })
                .ToList();

                // Export logic

                if (consulta.Formato.ToLower() == "pdf")
                {
                    var fileBytes = ExportToPdf(resultado);
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(fileBytes)
                    };
                    response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
                    response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                    {
                        FileName = "Reservas.pdf"
                    };
                    return ResponseMessage(response);
                }
                else
                {
                    return BadRequest("Formato no soportado.");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        #endregion

        #endregion

        #region Helper Methods

        /// <summary>
        /// Rechaza automáticamente solicitudes que entran en conflicto con una aprobada
        /// </summary>
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

        /// <summary>
        /// Actualiza el estado de una solicitud (aprobado/rechazado)
        /// </summary>
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

        /// <summary>
        /// Crea una nueva solicitud de reserva con validaciones
        /// </summary>
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
                    solicitud.UsuarioId,
                    solicitud.Fecha,
                    HoraInicio = solicitud.HoraInicio.ToString(@"hh\:mm"),
                    HoraFin = solicitud.HoraFin.ToString(@"hh\:mm"),
                    solicitud.Estado,
                    solicitud.Descripcion,
                    Usuario = new { usuario.Nombre, usuario.Apellido, usuario.Correo },
                    Espacio = new { espacio.Nombre, espacio.Codigo }
                });
        }

        /// <summary>
        /// Verifica si existe conflicto de horario para un espacio en una fecha y hora específica
        /// </summary>
        private bool ExisteConflictoHorario(int espacioId, DateTime fecha, TimeSpan horaInicio, TimeSpan horaFin)
        {
            return db.Solicitudes
                .Any(s => s.EspacioId == espacioId &&
                           DbFunctions.TruncateTime(s.Fecha) == DbFunctions.TruncateTime(fecha) &&
                           s.Estado != "rechazado" &&
                           s.HoraInicio < horaFin &&
                           s.HoraFin > horaInicio);
        }

        /// <summary>
        /// Exporta datos a formato Excel (no implementado completamente)
        /// </summary>
        private byte[] ExportToExcel(IEnumerable<object> data)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Reservas");

                // Header
                worksheet.Cell(1, 1).Value = "ID";
                worksheet.Cell(1, 2).Value = "Fecha";
                worksheet.Cell(1, 3).Value = "Hora Inicio";
                worksheet.Cell(1, 4).Value = "Hora Fin";
                worksheet.Cell(1, 5).Value = "Estado";
                worksheet.Cell(1, 6).Value = "Espacio";
                worksheet.Cell(1, 7).Value = "Usuario";

                int row = 2;
                foreach (var item in data)
                {
                    var id = item.GetType().GetProperty("Id")?.GetValue(item, null);
                    var fecha = ((DateTime)item.GetType().GetProperty("Fecha")?.GetValue(item, null)).ToString("dd/MM/yyyy");
                    var horaInicio = ((TimeSpan)item.GetType().GetProperty("HoraInicio")?.GetValue(item, null)).ToString(@"hh\:mm");
                    var horaFin = ((TimeSpan)item.GetType().GetProperty("HoraFin")?.GetValue(item, null)).ToString(@"hh\:mm");
                    var estado = item.GetType().GetProperty("Estado")?.GetValue(item, null);

                    var espacio = item.GetType().GetProperty("Espacio")?.GetValue(item, null);
                    var nombreEspacio = espacio?.GetType().GetProperty("Nombre")?.GetValue(espacio, null);
                    var tipoEspacio = espacio?.GetType().GetProperty("Tipo")?.GetValue(espacio, null);

                    var usuario = item.GetType().GetProperty("Usuario")?.GetValue(item, null);
                    var nombreUsuario = usuario?.GetType().GetProperty("Nombre")?.GetValue(usuario, null);
                    var apellidoUsuario = usuario?.GetType().GetProperty("Apellido")?.GetValue(usuario, null);

                    worksheet.Cell(row, 1).Value = XLCellValue.FromObject(id);
                    worksheet.Cell(row, 2).Value = fecha;
                    worksheet.Cell(row, 3).Value = horaInicio;
                    worksheet.Cell(row, 4).Value = horaFin;
                    worksheet.Cell(row, 5).Value = XLCellValue.FromObject(estado);
                    worksheet.Cell(row, 6).Value = $"{nombreEspacio} ({tipoEspacio})";
                    worksheet.Cell(row, 7).Value = $"{nombreUsuario} {apellidoUsuario}";

                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Exporta datos a formato PDF
        /// </summary>
        private byte[] ExportToPdf(IEnumerable<object> data)
        {
            using (var stream = new MemoryStream())
            {
                var doc = new Document(PageSize.A4, 25, 25, 30, 30);
                var writer = PdfWriter.GetInstance(doc, stream);
                doc.Open();

                // Title
                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);

                doc.Add(new Paragraph("Reporte de Reservas", titleFont));
                doc.Add(new Paragraph(" ")); // Empty line

                // Table
                PdfPTable table = new PdfPTable(7) { WidthPercentage = 100 };
                table.SetWidths(new float[] { 1f, 2f, 2f, 2f, 2f, 3f, 2f });

                // Header
                table.AddCell(new PdfPCell(new Phrase("ID", normalFont)));
                table.AddCell(new PdfPCell(new Phrase("Fecha", normalFont)));
                table.AddCell(new PdfPCell(new Phrase("Hora Inicio", normalFont)));
                table.AddCell(new PdfPCell(new Phrase("Hora Fin", normalFont)));
                table.AddCell(new PdfPCell(new Phrase("Estado", normalFont)));
                table.AddCell(new PdfPCell(new Phrase("Espacio", normalFont)));
                table.AddCell(new PdfPCell(new Phrase("Usuario", normalFont)));

                foreach (var item in data)
                {
                    var id = item.GetType().GetProperty("Id")?.GetValue(item, null)?.ToString();
                    var fecha = ((DateTime)item.GetType().GetProperty("Fecha")?.GetValue(item, null)).ToString("dd/MM/yyyy");

                    // Formatea los TimeSpan correctamente
                    var horaInicio = ((TimeSpan)item.GetType().GetProperty("HoraInicio")?.GetValue(item, null)).ToString(@"hh\:mm");
                    var horaFin = ((TimeSpan)item.GetType().GetProperty("HoraFin")?.GetValue(item, null)).ToString(@"hh\:mm");

                    var estado = item.GetType().GetProperty("Estado")?.GetValue(item, null)?.ToString();

                    var espacio = item.GetType().GetProperty("Espacio")?.GetValue(item, null);
                    var nombreEspacio = espacio?.GetType().GetProperty("Nombre")?.GetValue(espacio, null)?.ToString();
                    var tipoEspacio = espacio?.GetType().GetProperty("Tipo")?.GetValue(espacio, null)?.ToString();

                    var usuario = item.GetType().GetProperty("Usuario")?.GetValue(item, null);
                    var nombreUsuario = usuario?.GetType().GetProperty("Nombre")?.GetValue(usuario, null)?.ToString();
                    var apellidoUsuario = usuario?.GetType().GetProperty("Apellido")?.GetValue(usuario, null)?.ToString();

                    table.AddCell(new PdfPCell(new Phrase(id ?? "", normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(fecha ?? "", normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(horaInicio ?? "", normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(horaFin ?? "", normalFont)));
                    table.AddCell(new PdfPCell(new Phrase(estado ?? "", normalFont)));
                    table.AddCell(new PdfPCell(new Phrase($"{nombreEspacio} ({tipoEspacio})" ?? "", normalFont)));
                    table.AddCell(new PdfPCell(new Phrase($"{nombreUsuario} {apellidoUsuario}" ?? "", normalFont)));
                }

                doc.Add(table);
                doc.Close();

                return stream.ToArray();
            }
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

    public class ConsultaDisponibilidadDto
    {
        [Required(ErrorMessage = "La fecha es obligatoria")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(Solicitud), "ValidarFechaFutura")]
        public DateTime Fecha { get; set; }

        [Required(ErrorMessage = "Hora de inicio es obligatoria")]
        [DataType(DataType.Time)]
        public TimeSpan HoraInicio { get; set; }

        [Required(ErrorMessage = "Hora de fin es obligatoria")]
        [DataType(DataType.Time)]
        public TimeSpan HoraFin { get; set; }
    }

    public class ConsultaDisponibilidadConCredencialesDto
    {
        [Required]
        public CredencialesDto Credenciales { get; set; }

        [Required]
        public ConsultaDisponibilidadDto Consulta { get; set; }
    }

    public class EspacioDisponibleDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Codigo { get; set; }
        public string Tipo { get; set; }
        public bool Disponible { get; set; } = true;
    }
    #region ExamenFinalDTO
    public class ConsultaPeriodoDto
    {
        [Required]
        public CredencialesDto Credenciales { get; set; }
        [Required]
        public DateTime FechaInicio { get; set; }
        [Required]
        public DateTime FechaFin { get; set; }
        [Required]
        [RegularExpression("^(dia|semana|mes)$", ErrorMessage = "TipoPeriodo debe ser 'dia', 'semana' o 'mes'")]
        public string TipoPeriodo { get; set; }
    }
    public class ConsultaAvanzadaDto
    {
        [Required]
        public CredencialesDto Credenciales { get; set; }
        public int? UsuarioId { get; set; }
        public string TipoEspacio { get; set; }
        public string Estado { get; set; }
    }

    public class ExportarConsultaDto : ConsultaAvanzadaDto
    {
        [Required]
        [RegularExpression("^(pdf)$", ErrorMessage = "Formato debe ser 'pdf'")]
        public string Formato { get; set; }

    }

    #endregion
}