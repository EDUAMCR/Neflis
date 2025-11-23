using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neflis.Migrations
{
    /// <inheritdoc />
    public partial class CalificacionesContenido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalificacionesContenido",
                columns: table => new
                {
                    CalificacionContenidoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContenidoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Estrellas = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalificacionesContenido", x => x.CalificacionContenidoId);
                    table.ForeignKey(
                        name: "FK_CalificacionesContenido_Contenidos_ContenidoId",
                        column: x => x.ContenidoId,
                        principalTable: "Contenidos",
                        principalColumn: "ContenidoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CalificacionesContenido_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "UsuarioId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesContenido_ContenidoId",
                table: "CalificacionesContenido",
                column: "ContenidoId");

            migrationBuilder.CreateIndex(
                name: "IX_CalificacionesContenido_UsuarioId",
                table: "CalificacionesContenido",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalificacionesContenido");
        }
    }
}
