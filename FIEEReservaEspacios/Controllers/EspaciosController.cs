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
    [RoutePrefix("FIEE/espacios")]
    public class EspaciosController : ApiController
    {
        private readonly ReservaEspaciosContext db = new ReservaEspaciosContext();

        // POST FIEE/espacios
        [HttpPost]
        [Route("")] // Matches the prefix
        public IHttpActionResult CrearEspacio([FromBody] Espacio espacio)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (db.Espacios.Any(e => e.Codigo == espacio.Codigo))
                    return Content(HttpStatusCode.Conflict, "El código ya está registrado");

                db.Espacios.Add(espacio);
                db.SaveChanges();

                // Return 201 Created with location header
                return Created(
                    new Uri($"{Request.RequestUri}/{espacio.Id}"),
                    new
                    {
                        espacio.Id,
                        espacio.Nombre,
                        espacio.Codigo,
                        espacio.Tipo
                        // Exclude any sensitive/non-necessary fields
                    });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // GET FIEE/espacios
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

        // GET FIEE/espacios/5
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

        // PUT FIEE/espacios/5
        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult ActualizarEspacio(int id, [FromBody] Espacio espacio)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (id != espacio.Id)
                    return BadRequest("ID inconsistente");

                var existente = db.Espacios.Find(id);
                if (existente == null)
                    return NotFound();

                if (db.Espacios.Any(e => e.Id != id && e.Codigo == espacio.Codigo))
                    return Content(HttpStatusCode.Conflict, "El código ya está registrado");

                // Update properties
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

        // DELETE FIEE/espacios/5
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
