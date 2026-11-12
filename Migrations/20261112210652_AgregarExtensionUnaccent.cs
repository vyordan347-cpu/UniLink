using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniLink.Migrations
{
    /// <inheritdoc />
    public partial class AgregarExtensionUnaccent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS unaccent;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
