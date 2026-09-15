using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Woodpecker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Puzzles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fen = table.Column<string>(type: "text", nullable: false),
                    SolutionMoves = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Puzzles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PuzzleSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuzzleSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PuzzleSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleNumber = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PuzzleSetItems",
                columns: table => new
                {
                    PuzzleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PuzzleSetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuzzleSetItems", x => new { x.PuzzleSetId, x.PuzzleId });
                    table.ForeignKey(
                        name: "FK_PuzzleSetItems_PuzzleSets_PuzzleSetId",
                        column: x => x.PuzzleSetId,
                        principalTable: "PuzzleSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PuzzleSetItems_Puzzles_PuzzleId",
                        column: x => x.PuzzleId,
                        principalTable: "Puzzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PuzzleAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingCycleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PuzzleId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PuzzleAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PuzzleAttempts_TrainingCycles_TrainingCycleId",
                        column: x => x.TrainingCycleId,
                        principalTable: "TrainingCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PuzzleAttempts_PuzzleId",
                table: "PuzzleAttempts",
                column: "PuzzleId");

            migrationBuilder.CreateIndex(
                name: "IX_PuzzleAttempts_TrainingCycleId",
                table: "PuzzleAttempts",
                column: "TrainingCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_PuzzleSetItems_PuzzleId",
                table: "PuzzleSetItems",
                column: "PuzzleId");

            migrationBuilder.CreateIndex(
                name: "IX_PuzzleSets_OwnerId",
                table: "PuzzleSets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingCycles_PuzzleSetId",
                table: "TrainingCycles",
                column: "PuzzleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingCycles_UserId",
                table: "TrainingCycles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PuzzleAttempts");

            migrationBuilder.DropTable(
                name: "PuzzleSetItems");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "TrainingCycles");

            migrationBuilder.DropTable(
                name: "PuzzleSets");

            migrationBuilder.DropTable(
                name: "Puzzles");
        }
    }
}
