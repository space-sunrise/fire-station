using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class AddMetaGarbage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "meta_garbage",
                columns: table => new
                {
                    meta_garbage_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    map_prototype = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    data = table.Column<string>(type: "text", nullable: false),
                    map_version = table.Column<int>(type: "integer", nullable: false),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meta_garbage", x => x.meta_garbage_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_meta_garbage_map_prototype",
                table: "meta_garbage",
                column: "map_prototype",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "meta_garbage");
        }
    }
}
