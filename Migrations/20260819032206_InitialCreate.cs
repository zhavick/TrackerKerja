using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TrackerKerja.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Deadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JsonHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    TaskId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JsonHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JsonHistories_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Duration = table.Column<long>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Color", "Name" },
                values: new object[,]
                {
                    { 1, "#6366F1", "Backend" },
                    { 2, "#06B6D4", "Frontend" },
                    { 3, "#10B981", "API / REST" },
                    { 4, "#F59E0B", "Database" },
                    { 5, "#EF4444", "DevOps" },
                    { 6, "#8B5CF6", "Testing" }
                });

            migrationBuilder.InsertData(
                table: "Projects",
                columns: new[] { "Id", "Color", "CreatedAt", "Deadline", "Description", "Name", "Status" },
                values: new object[,]
                {
                    { 1, "#6366F1", new DateTime(2026, 8, 19, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2328), new DateTime(2026, 10, 19, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2318), "Aplikasi tracker kerja all-in-one", "Work Tracker Pro", 0 },
                    { 2, "#10B981", new DateTime(2026, 8, 19, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2332), new DateTime(2026, 9, 19, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2331), "Integrasi REST API dengan sistem eksternal", "REST API Integration", 0 }
                });

            migrationBuilder.InsertData(
                table: "Tasks",
                columns: new[] { "Id", "CategoryId", "CreatedAt", "Description", "DueDate", "Priority", "ProjectId", "StartDate", "Status", "Tags", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 8, 19, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2363), "Inisialisasi project dengan EF Core dan SQLite", new DateTime(2026, 8, 20, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2359), 2, 1, null, 2, null, "Setup Project ASP.NET Core MVC", new DateTime(2026, 8, 19, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2364) },
                    { 2, 2, new DateTime(2026, 8, 19, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2368), "Membuat tampilan dashboard dengan Tailwind CSS", new DateTime(2026, 8, 22, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2367), 2, 1, null, 1, null, "Design UI Dashboard", new DateTime(2026, 8, 19, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2368) },
                    { 3, 3, new DateTime(2026, 8, 19, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2371), "Mempelajari dan mendokumentasikan endpoint REST", new DateTime(2026, 8, 24, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2370), 1, 2, null, 0, null, "Analisis endpoint REST API", new DateTime(2026, 8, 19, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2371) },
                    { 4, 3, new DateTime(2026, 8, 19, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2374), "Memverifikasi format response JSON dari API", new DateTime(2026, 8, 26, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2374), 1, 2, null, 0, null, "Testing JSON Response Format", new DateTime(2026, 8, 19, 10, 22, 6, 69, DateTimeKind.Local).AddTicks(2375) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_JsonHistories_TaskId",
                table: "JsonHistories",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TaskId",
                table: "Sessions",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CategoryId",
                table: "Tasks",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ProjectId",
                table: "Tasks",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JsonHistories");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
