using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScadAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Intent",
                table: "Messages",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Intent",
                table: "Messages");
        }
    }
}
