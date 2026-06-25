using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScadAgent.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionContextSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ContextSummarizedThroughMessageId",
                table: "Sessions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContextSummary",
                table: "Sessions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContextSummarizedThroughMessageId",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "ContextSummary",
                table: "Sessions");
        }
    }
}
