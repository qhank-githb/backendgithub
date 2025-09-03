using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinioWebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSerilogLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_info",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    stored_file_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    original_file_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    bucketname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    relative_path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    absolute_path = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    upload_time = table.Column<DateTime>(type: "datetime2", nullable: false),
                    uploader = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    etag = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_info", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "SerilogLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Properties = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerilogLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "file_tags",
                columns: table => new
                {
                    FileId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_tags", x => new { x.FileId, x.TagId });
                    table.ForeignKey(
                        name: "FK_file_tags_file_info_FileId",
                        column: x => x.FileId,
                        principalTable: "file_info",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_file_tags_tags_TagId",
                        column: x => x.TagId,
                        principalTable: "tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_file_tags_TagId",
                table: "file_tags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_SerilogLogs_Timestamp",
                table: "SerilogLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_tags_Name",
                table: "tags",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_tags");

            migrationBuilder.DropTable(
                name: "SerilogLogs");

            migrationBuilder.DropTable(
                name: "file_info");

            migrationBuilder.DropTable(
                name: "tags");
        }
    }
}
