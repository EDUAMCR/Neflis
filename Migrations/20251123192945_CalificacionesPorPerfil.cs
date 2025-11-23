using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neflis.Migrations
{
    /// <inheritdoc />
    public partial class CalificacionesPorPerfil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 0) Limpio calificaciones antiguas (estaban por Usuario, sin Perfil)
            //    Solo en entorno de dev / pruebas.
            migrationBuilder.Sql("DELETE FROM CalificacionesContenido");

            // 1) Quitamos el índice antiguo (solo por ContenidoId)
            migrationBuilder.DropIndex(
                name: "IX_CalificacionesContenido_ContenidoId",
                table: "CalificacionesContenido");

            // 2) Agregamos la nueva columna PerfilId (obligatoria)
            migrationBuilder.AddColumn<int>(
                name: "PerfilId",
                table: "CalificacionesContenido",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 3) Índice único por Contenido + Perfil (una calificación por perfil)
            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesContenido_ContenidoId_PerfilId",
                table: "CalificacionesContenido",
                columns: new[] { "ContenidoId", "PerfilId" },
                unique: true);

            // 4) Índice solo por PerfilId (para buscar rápido “mis” calificaciones)
            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesContenido_PerfilId",
                table: "CalificacionesContenido",
                column: "PerfilId");

            // 5) Foreign key hacia Perfiles SIN cascada (para evitar multiple paths)
            migrationBuilder.AddForeignKey(
                name: "FK_CalificacionesContenido_Perfiles_PerfilId",
                table: "CalificacionesContenido",
                column: "PerfilId",
                principalTable: "Perfiles",
                principalColumn: "PerfilId",
                onDelete: ReferentialAction.NoAction); // o Restrict
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalificacionesContenido_Perfiles_PerfilId",
                table: "CalificacionesContenido");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesContenido_ContenidoId_PerfilId",
                table: "CalificacionesContenido");

            migrationBuilder.DropIndex(
                name: "IX_CalificacionesContenido_PerfilId",
                table: "CalificacionesContenido");

            migrationBuilder.DropColumn(
                name: "PerfilId",
                table: "CalificacionesContenido");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesContenido_ContenidoId",
                table: "CalificacionesContenido",
                column: "ContenidoId");
        }
    }
}
