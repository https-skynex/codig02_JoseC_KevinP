using FIEEReservaEspacios.DAL;
using FIEEReservaEspacios.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SistemaReservasEspaciosFIEE.Controllers
{
    [RoutePrefix("FIEE/usuarios")]
    public class UsuariosController : ApiController
    {
        private readonly ReservaEspaciosContext db = new ReservaEspaciosContext();

        /// <summary>
        /// Obtiene todos los usuarios registrados (sin información sensible como contraseñas)
        /// </summary>
        /// <returns>Lista de usuarios con información básica</returns>
        [HttpGet]
        [Route("")]
        public IHttpActionResult ObtenerTodosUsuarios()
        {
            try
            {
                var usuarios = db.Usuarios.Select(u => new
                {
                    u.Id,
                    u.Nombre,
                    u.Apellido,
                    u.Correo,
                    u.Rol
                    // No incluir contraseña en la respuesta
                }).ToList();

                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Obtiene un usuario específico por su ID
        /// </summary>
        /// <param name="id">ID del usuario a buscar</param>
        /// <returns>Información del usuario o NotFound si no existe</returns>
        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult ObtenerUsuarioPorId(int id)
        {
            try
            {
                var usuario = db.Usuarios
                    .Where(u => u.Id == id)
                    .Select(u => new
                    {
                        u.Id,
                        u.Nombre,
                        u.Apellido,
                        u.Correo,
                        u.Rol
                    }).FirstOrDefault();

                if (usuario == null)
                {
                    return NotFound();
                }

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Crea un nuevo usuario en el sistema
        /// </summary>
        /// <param name="usuarioDto">Datos del nuevo usuario</param>
        /// <returns>Usuario creado (sin contraseña) o error de validación</returns>
        [HttpPost]
        [Route("")]
        public IHttpActionResult CrearUsuario([FromBody] UsuarioCreacionDto usuarioDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validar que el correo no esté registrado
                if (db.Usuarios.Any(u => u.Correo == usuarioDto.Correo))
                {
                    return Content(HttpStatusCode.Conflict, "El correo electrónico ya está registrado");
                }

                var usuario = new Usuario
                {
                    Nombre = usuarioDto.Nombre,
                    Apellido = usuarioDto.Apellido,
                    Correo = usuarioDto.Correo,
                    Contrasena = usuarioDto.Contrasena,
                    Rol = usuarioDto.Rol
                };

                db.Usuarios.Add(usuario);
                db.SaveChanges();

                // Respuesta sin contraseña por seguridad
                var respuesta = new
                {
                    usuario.Id,
                    usuario.Nombre,
                    usuario.Apellido,
                    usuario.Correo,
                    usuario.Rol
                };

                return Created(
                    new Uri($"{Request.RequestUri}/{usuario.Id}"),
                    respuesta
                );
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Actualiza la información de un usuario existente
        /// </summary>
        /// <param name="id">ID del usuario a actualizar</param>
        /// <param name="usuarioDto">Nuevos datos del usuario</param>
        /// <returns>NoContent si éxito, o error si falla</returns>
        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult ActualizarUsuario(int id, [FromBody] UsuarioActualizacionDto usuarioDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var usuarioExistente = db.Usuarios.Find(id);
                if (usuarioExistente == null)
                {
                    return NotFound();
                }

                // Validar que el correo no esté siendo usado por otro usuario
                if (db.Usuarios.Any(u => u.Id != id && u.Correo == usuarioDto.Correo))
                {
                    return Content(HttpStatusCode.Conflict, "El correo electrónico ya está registrado");
                }

                // Actualizar solo campos permitidos
                usuarioExistente.Nombre = usuarioDto.Nombre;
                usuarioExistente.Apellido = usuarioDto.Apellido;
                usuarioExistente.Correo = usuarioDto.Correo;
                usuarioExistente.Rol = usuarioDto.Rol;

                // Actualizar contraseña solo si se proporcionó una nueva
                if (!string.IsNullOrEmpty(usuarioDto.Contrasena))
                {
                    usuarioExistente.Contrasena = usuarioDto.Contrasena;
                }

                db.Entry(usuarioExistente).State = EntityState.Modified;
                db.SaveChanges();

                return StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Elimina un usuario del sistema
        /// </summary>
        /// <param name = "id">ID del usuario a eliminar</param>
        /// <returns>Mensaje de confirmación o error</returns>
        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult EliminarUsuario(int id)
        {
            try
            {
                var usuario = db.Usuarios.Find(id);
                if (usuario == null)
                {
                    return NotFound();
                }

                db.Usuarios.Remove(usuario);
                db.SaveChanges();

                return Ok(new
                {
                    message = "Usuario eliminado correctamente",
                    id = usuario.Id
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// DTO para creación de usuarios con validaciones
        /// </summary>
        public class UsuarioCreacionDto
        {
            [Required]
            [StringLength(50)]
            public string Nombre { get; set; }

            [Required]
            [StringLength(50)]
            public string Apellido { get; set; }

            [Required]
            [EmailAddress]
            public string Correo { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 8)]
            public string Contrasena { get; set; }

            [Required]
            public string Rol { get; set; }
        }

        /// <summary>
        /// DTO para actualización de usuarios con validaciones
        /// </summary>
        public class UsuarioActualizacionDto
        {
            [StringLength(50)]
            public string Nombre { get; set; }

            [StringLength(50)]
            public string Apellido { get; set; }

            [EmailAddress]
            public string Correo { get; set; }

            [StringLength(100, MinimumLength = 8)]
            public string Contrasena { get; set; }

            public string Rol { get; set; }
        }
    }
}
