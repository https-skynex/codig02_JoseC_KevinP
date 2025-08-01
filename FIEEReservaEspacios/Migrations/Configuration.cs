namespace FIEEReservaEspacios.Migrations
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Data.Entity.Validation;
    using System.Linq;
    using FIEEReservaEspacios.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<FIEEReservaEspacios.DAL.ReservaEspaciosContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(FIEEReservaEspacios.DAL.ReservaEspaciosContext context)
        {
            try
            {
                // 1. Seed de Usuarios
                var usuarios = new List<Usuario>
                {
                    new Usuario
                    {
                        Nombre = "Admin",
                        Apellido = "Sistema",
                        Correo = "admin@epn.edu.ec",
                        Contrasena = "Admin123!",
                        Rol = "Administrador"
                    },
                    new Usuario
                    {
                        Nombre = "Juan",
                        Apellido = "Pérez",
                        Correo = "jperez@epn.edu.ec",
                        Contrasena = "Prof123!",
                        Rol = "Profesor"
                    }
                };

                foreach (var usuario in usuarios)
                {
                    if (!context.Usuarios.Any(u => u.Correo == usuario.Correo))
                    {
                        context.Usuarios.Add(usuario);
                    }
                }
                context.SaveChanges();

                // 2. Seed de Espacios
                var espacios = new List<Espacio>
                {
                    new Espacio
                    {
                        Nombre = "Aula 101",
                        Numero_Edificio = "1",
                        Piso = 1,
                        Codigo = "E01/P1/E101",
                        Tipo = "aula"
                    },
                    new Espacio
                    {
                        Nombre = "Laboratorio de Redes",
                        Numero_Edificio = "17",
                        Piso = 3,
                        Codigo = "E17/P3/E201",
                        Tipo = "laboratorio"
                    },
                    new Espacio
                    {
                        Nombre = "Auditorio Principal",
                        Numero_Edificio = "1",
                        Piso = 0,
                        Codigo = "E01/P0/E001",
                        Tipo = "auditorio"
                    }
                };

                foreach (var espacio in espacios)
                {
                    if (!context.Espacios.Any(e => e.Codigo == espacio.Codigo))
                    {
                        context.Espacios.Add(espacio);
                    }
                }
                context.SaveChanges();

                // 3. Obtener IDs reales
                var admin = context.Usuarios.First(u => u.Correo == "admin@epn.edu.ec");
                var profesor = context.Usuarios.First(u => u.Correo == "jperez@epn.edu.ec");
                var aula = context.Espacios.First(e => e.Codigo == "E01/P1/E101");
                var laboratorio = context.Espacios.First(e => e.Codigo == "E17/P3/E201");

                // 4. Seed de Solicitudes con validación mejorada
                var solicitudes = new List<Solicitud>
                {
                    new Solicitud
                    {
                        UsuarioId = admin.Id,
                        EspacioId = aula.Id,
                        Fecha = DateTime.Today.AddDays(3),
                        HoraInicio = new TimeSpan(9, 0, 0),
                        HoraFin = new TimeSpan(11, 0, 0),
                        Descripcion = "Reunión de planificación",
                        Estado = "pendiente"
                    },
                    new Solicitud
                    {
                        UsuarioId = profesor.Id,
                        EspacioId = laboratorio.Id,
                        Fecha = DateTime.Today.AddDays(3),
                        HoraInicio = new TimeSpan(14, 0, 0),
                        HoraFin = new TimeSpan(16, 0, 0),
                        Descripcion = "Práctica de redes",
                        Estado = "aprobado"
                    }
                };

                foreach (var solicitud in solicitudes)
                {
                    // Verificar conflicto usando TimeSpan
                    bool conflicto = context.Solicitudes.Any(s =>
                        s.EspacioId == solicitud.EspacioId &&
                        DbFunctions.TruncateTime(s.Fecha) == DbFunctions.TruncateTime(solicitud.Fecha) &&
                        s.Estado != "rechazado" &&
                        s.HoraInicio < solicitud.HoraFin &&
                        s.HoraFin > solicitud.HoraInicio);

                    if (!conflicto)
                    {
                        var validationContext = new ValidationContext(solicitud);
                        var validationResults = new List<ValidationResult>();

                        if (Validator.TryValidateObject(solicitud, validationContext, validationResults, true))
                        {
                            context.Solicitudes.Add(solicitud);
                        }
                        else
                        {
                            var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
                            System.Diagnostics.Debug.WriteLine($"Error validando solicitud: {errors}");
                        }
                    }
                }
                context.SaveChanges();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                    }
                }
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Seed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }
    }
}