using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UniLink.Migrations
{
    /// <inheritdoc />
    public partial class ResenasSentimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Resenas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AutorId = table.Column<int>(type: "integer", nullable: false),
                    UsuarioReseñadoId = table.Column<int>(type: "integer", nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: true),
                    ClaseId = table.Column<int>(type: "integer", nullable: true),
                    AlquilerId = table.Column<int>(type: "integer", nullable: true),
                    ReservaClaseId = table.Column<int>(type: "integer", nullable: true),
                    Calificacion = table.Column<int>(type: "integer", nullable: false),
                    Comentario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Sentimiento = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Probabilidad = table.Column<float>(type: "real", nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resenas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resenas_Alquileres_AlquilerId",
                        column: x => x.AlquilerId,
                        principalTable: "Alquileres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resenas_AspNetUsers_AutorId",
                        column: x => x.AutorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resenas_AspNetUsers_UsuarioReseñadoId",
                        column: x => x.UsuarioReseñadoId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resenas_Clases_ClaseId",
                        column: x => x.ClaseId,
                        principalTable: "Clases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resenas_Materiales_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materiales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resenas_ReservasClase_ReservaClaseId",
                        column: x => x.ReservaClaseId,
                        principalTable: "ReservasClase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_AlquilerId",
                table: "Resenas",
                column: "AlquilerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_AutorId",
                table: "Resenas",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_ClaseId",
                table: "Resenas",
                column: "ClaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_MaterialId",
                table: "Resenas",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_ReservaClaseId",
                table: "Resenas",
                column: "ReservaClaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resenas_UsuarioReseñadoId",
                table: "Resenas",
                column: "UsuarioReseñadoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Resenas");
        }
    }
}
