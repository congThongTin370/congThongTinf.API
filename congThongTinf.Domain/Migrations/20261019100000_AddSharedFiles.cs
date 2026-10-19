using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace congThongTinf.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SharedFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FileType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    FileKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShareToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedFiles_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedFiles_ShareToken",
                table: "SharedFiles",
                column: "ShareToken",
                unique: true,
                filter: "\"ShareToken\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SharedFiles_UploadedByUserId",
                table: "SharedFiles",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedFiles");
        }
    }
}
