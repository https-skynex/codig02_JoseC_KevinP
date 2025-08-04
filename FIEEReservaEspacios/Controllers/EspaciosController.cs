using FIEEReservaEspacios.DAL;
using FIEEReservaEspacios.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FIEEReservaEspacios.Controllers
{
    /// <summary>
    /// Controlador API para la gestión de espacios físicos
    /// </summary>
    [RoutePrefix("FIEE/espacios")]
    public class EspaciosController : ApiController
    {
        private readonly ReservaEspaciosContext db = new ReservaEspaciosContext();

        /// <summary>
        /// Crea un nuevo espacio en el sistema
        /// </summary>
        /// <param name="espacio">Datos del espacio a crear</param>
        /// <returns>Respuesta HTTP con el espacio creado o mensaje de error</returns>
        [HttpPost]
        [Route("")]
        public IHttpActionResult CrearEspacio([FromBody] Espacio espacio)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Verificar que el código del espacio no esté duplicado
                if (db.Espacios.Any(e => e.Codigo == espacio.Codigo))
                    return Content(HttpStatusCode.Conflict, "El código ya está registrado");

                db.Espacios.Add(espacio);
                db.SaveChanges();

                // Retorna respuesta 201 Created con URL del nuevo recurso
                return Created(
                    new Uri($"{Request.RequestUri}/{espacio.Id}"),
                    new
                    {
                        espacio.Id,
                        espacio.Nombre,
                        espacio.Codigo,
                        espacio.Tipo
                        // No incluir información sensible o innecesaria
                    });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Obtiene todos los espacios registrados en el sistema
        /// </summary>
        /// <returns>Lista de espacios con información básica</returns>
        [HttpGet]
        [Route("")]
        public IHttpActionResult ObtenerTodosEspacios()
        {
            try
            {
                var espacios = db.Espacios
                    .Select(e => new {
                        e.Id,
                        e.Nombre,
                        e.Codigo,
                        e.Tipo,
                        e.Numero_Edificio,
                        e.Piso
                    })
                    .ToList();

                return Ok(espacios);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Obtiene un espacio específico por su ID
        /// </summary>
        /// <param name="id">ID del espacio a buscar</param>
        /// <returns>Información del espacio o NotFound si no existe</returns>
        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult ObtenerEspacioPorId(int id)
        {
            try
            {
                var espacio = db.Espacios
                    .Where(e => e.Id == id)
                    .Select(e => new {
                        e.Id,
                        e.Nombre,
                        e.Codigo,
                        e.Tipo,
                        e.Numero_Edificio,
                        e.Piso
                    })
                    .FirstOrDefault();

                if (espacio == null)
                    return NotFound();

                return Ok(espacio);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Actualiza la información de un espacio existente
        /// </summary>
        /// <param name="id">ID del espacio a actualizar</param>
        /// <param name="espacio">Nuevos datos del espacio</param>
        /// <returns>NoContent si éxito, o error si falla</returns>
        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult ActualizarEspacio(int id, [FromBody] Espacio espacio)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Validar consistencia de IDs
                if (id != espacio.Id)
                    return BadRequest("ID inconsistente");

                var existente = db.Espacios.Find(id);
                if (existente == null)
                    return NotFound();

                // Validar que el código no esté siendo usado por otro espacio
                if (db.Espacios.Any(e => e.Id != id && e.Codigo == espacio.Codigo))
                    return Content(HttpStatusCode.Conflict, "El código ya está registrado");

                // Actualizar propiedades permitidas
                existente.Nombre = espacio.Nombre;
                existente.Codigo = espacio.Codigo;
                existente.Tipo = espacio.Tipo;
                existente.Numero_Edificio = espacio.Numero_Edificio;
                existente.Piso = espacio.Piso;

                db.Entry(existente).State = EntityState.Modified;
                db.SaveChanges();

                return StatusCode(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Elimina un espacio del sistema
        /// </summary>
        /// <param name="id">ID del espacio a eliminar</param>
        /// <returns>Mensaje de confirmación o error</returns>
        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult EliminarEspacio(int id)
        {
            try
            {
                var espacio = db.Espacios.Find(id);
                if (espacio == null)
                    return NotFound();

                db.Espacios.Remove(espacio);
                db.SaveChanges();

                return Ok(new
                {
                    message = "Espacio eliminado correctamente",
                    id = espacio.Id
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
