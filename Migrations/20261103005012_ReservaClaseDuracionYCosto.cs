using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniLink.Migrations
{
    /// <inheritdoc />
    public partial class ReservaClaseDuracionYCosto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostoTotal",
                table: "ReservasClase",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DuracionHoras",
                table: "ReservasClase",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostoTotal",
                table: "ReservasClase");

            migrationBuilder.DropColumn(
                name: "DuracionHoras",
                table: "ReservasClase");
        }
    }
}
