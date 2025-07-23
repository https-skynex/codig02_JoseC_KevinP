namespace FIEEReservaEspacios.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateSolicitud : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Solicitud", "HoraInicio", c => c.Time(nullable: false, precision: 7));
            AlterColumn("dbo.Solicitud", "HoraFin", c => c.Time(nullable: false, precision: 7));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Solicitud", "HoraFin", c => c.String(nullable: false));
            AlterColumn("dbo.Solicitud", "HoraInicio", c => c.String(nullable: false));
        }
    }
}
