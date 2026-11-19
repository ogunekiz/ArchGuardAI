using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArchGuard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BimProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BimProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BimProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ElementGlobalId = table.Column<string>(type: "text", nullable: false),
                    ElementType = table.Column<string>(type: "text", nullable: false),
                    ElementName = table.Column<string>(type: "text", nullable: false),
                    StoreyName = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    RuleReference = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SuggestedFix = table.Column<string>(type: "text", nullable: false),
                    LlmProviderUsed = table.Column<string>(type: "text", nullable: false),
                    AuditedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditFindings_BimProjects_BimProjectId",
                        column: x => x.BimProjectId,
                        principalTable: "BimProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BimDoors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BimProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    GlobalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StoreyName = table.Column<string>(type: "text", nullable: false),
                    WidthMeters = table.Column<double>(type: "double precision", nullable: false),
                    HeightMeters = table.Column<double>(type: "double precision", nullable: false),
                    IsFireExit = table.Column<bool>(type: "boolean", nullable: false),
                    FireRating = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BimDoors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BimDoors_BimProjects_BimProjectId",
                        column: x => x.BimProjectId,
                        principalTable: "BimProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BimSpaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BimProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    GlobalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    StoreyName = table.Column<string>(type: "text", nullable: false),
                    AreaM2 = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BimSpaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BimSpaces_BimProjects_BimProjectId",
                        column: x => x.BimProjectId,
                        principalTable: "BimProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BimStoreys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BimProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    GlobalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Elevation = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BimStoreys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BimStoreys_BimProjects_BimProjectId",
                        column: x => x.BimProjectId,
                        principalTable: "BimProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BimWalls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BimProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    GlobalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StoreyName = table.Column<string>(type: "text", nullable: false),
                    IsExternal = table.Column<bool>(type: "boolean", nullable: false),
                    LengthMeters = table.Column<double>(type: "double precision", nullable: false),
                    AreaM2 = table.Column<double>(type: "double precision", nullable: false),
                    VolumeM3 = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BimWalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BimWalls_BimProjects_BimProjectId",
                        column: x => x.BimProjectId,
                        principalTable: "BimProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_BimProjectId",
                table: "AuditFindings",
                column: "BimProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_BimDoors_BimProjectId",
                table: "BimDoors",
                column: "BimProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_BimSpaces_BimProjectId",
                table: "BimSpaces",
                column: "BimProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_BimStoreys_BimProjectId",
                table: "BimStoreys",
                column: "BimProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_BimWalls_BimProjectId",
                table: "BimWalls",
                column: "BimProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditFindings");

            migrationBuilder.DropTable(
                name: "BimDoors");

            migrationBuilder.DropTable(
                name: "BimSpaces");

            migrationBuilder.DropTable(
                name: "BimStoreys");

            migrationBuilder.DropTable(
                name: "BimWalls");

            migrationBuilder.DropTable(
                name: "BimProjects");
        }
    }
}
