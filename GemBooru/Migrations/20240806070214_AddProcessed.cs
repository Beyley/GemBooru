using GemBooru.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GemBooru.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GemBooruDatabaseContext))]
    [Migration("20240806070214_AddProcessed")]
    public partial class AddProcessed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Processed",
                table: "Posts",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Processed",
                table: "Posts");
        }
    }
}
