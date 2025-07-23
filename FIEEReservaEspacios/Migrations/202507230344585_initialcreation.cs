namespace FIEEReservaEspacios.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class initialcreation : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Espacio",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 100),
                        Numero_Edificio = c.String(nullable: false),
                        Piso = c.Int(nullable: false),
                        Codigo = c.String(nullable: false),
                        Tipo = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Solicitud",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UsuarioId = c.Int(nullable: false),
                        EspacioId = c.Int(nullable: false),
                        Fecha = c.DateTime(nullable: false),
                        HoraInicio = c.String(nullable: false),
                        HoraFin = c.String(nullable: false),
                        Descripcion = c.String(nullable: false, maxLength: 500),
                        Estado = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Espacio", t => t.EspacioId)
                .ForeignKey("dbo.Usuario", t => t.UsuarioId)
                .Index(t => t.UsuarioId)
                .Index(t => t.EspacioId);
            
            CreateTable(
                "dbo.Usuario",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Nombre = c.String(nullable: false, maxLength: 50),
                        Apellido = c.String(nullable: false, maxLength: 50),
                        Correo = c.String(nullable: false),
                        Contrasena = c.String(nullable: false, maxLength: 100),
                        Rol = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Solicitud", "UsuarioId", "dbo.Usuario");
            DropForeignKey("dbo.Solicitud", "EspacioId", "dbo.Espacio");
            DropIndex("dbo.Solicitud", new[] { "EspacioId" });
            DropIndex("dbo.Solicitud", new[] { "UsuarioId" });
            DropTable("dbo.Usuario");
            DropTable("dbo.Solicitud");
            DropTable("dbo.Espacio");
        }
    }
}
