using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniLink.Migrations
{
    /// <inheritdoc />
    public partial class EnlacesReunionPorReserva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnlaceReunion",
                table: "ReservasClase",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "ReservasClase" AS reserva
                SET "EnlaceReunion" = clase."EnlaceReunion"
                FROM "Clases" AS clase
                WHERE reserva."ClaseId" = clase."Id"
                    AND clase."EnlaceReunion" <> '';
                """);

            migrationBuilder.DropColumn(
                name: "EnlaceReunion",
                table: "Clases");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnlaceReunion",
                table: "ReservasClase");

            migrationBuilder.AddColumn<string>(
                name: "EnlaceReunion",
                table: "Clases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }
    }
}
