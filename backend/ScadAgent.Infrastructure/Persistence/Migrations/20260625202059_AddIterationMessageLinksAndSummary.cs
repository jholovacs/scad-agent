using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScadAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIterationMessageLinksAndSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IterationId",
                table: "Messages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Iterations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_IterationId",
                table: "Messages",
                column: "IterationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Iterations_IterationId",
                table: "Messages",
                column: "IterationId",
                principalTable: "Iterations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Iterations_IterationId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_IterationId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IterationId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Iterations");
        }
    }
}
