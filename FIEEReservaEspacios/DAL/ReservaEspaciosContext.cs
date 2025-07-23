using FIEEReservaEspacios.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Web;

namespace FIEEReservaEspacios.DAL
{
    public class ReservaEspaciosContext : DbContext
    {
        public ReservaEspaciosContext() : base("ReservaContext") { }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Configurar nombres de tablas en singular
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            base.OnModelCreating(modelBuilder);

            // Configuración de relaciones
            modelBuilder.Entity<Solicitud>()
                .HasRequired(s => s.Usuario)
                .WithMany()
                .HasForeignKey(s => s.UsuarioId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Solicitud>()
                .HasRequired(s => s.Espacio)
                .WithMany()
                .HasForeignKey(s => s.EspacioId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Usuario>().ToTable("Usuario");
            modelBuilder.Entity<Espacio>().ToTable("Espacio");
            modelBuilder.Entity<Solicitud>().ToTable("Solicitud");

            // Configurar mapeo de TimeSpan a time en SQL
            modelBuilder.Entity<Solicitud>()
                .Property(s => s.HoraInicio)
                .HasColumnType("time");

            modelBuilder.Entity<Solicitud>()
                .Property(s => s.HoraFin)
                .HasColumnType("time");
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Espacio> Espacios { get; set; }
        public DbSet<Solicitud> Solicitudes { get; set; }
    }
}