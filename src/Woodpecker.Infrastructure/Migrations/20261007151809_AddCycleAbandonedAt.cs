using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woodpecker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCycleAbandonedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AbandonedAt",
                table: "TrainingCycles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbandonedAt",
                table: "TrainingCycles");
        }
    }
}
