using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScadAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIterationLinearUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScadUnits",
                table: "Iterations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StlExportUnits",
                table: "Iterations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScadUnits",
                table: "Iterations");

            migrationBuilder.DropColumn(
                name: "StlExportUnits",
                table: "Iterations");
        }
    }
}
