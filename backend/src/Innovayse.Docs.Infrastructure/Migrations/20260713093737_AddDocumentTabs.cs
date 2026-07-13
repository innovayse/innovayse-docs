using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innovayse.Docs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentTabs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentTabs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    OrderIndex = table.Column<int>(type: "integer", nullable: false),
                    ContentSnapshot = table.Column<byte[]>(type: "bytea", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTabs", x => x.Id);
                });

            // Every existing Document becomes a single "Tab 1" carrying its current content.
            // The migrated tab reuses the document's own Id, so existing DocumentUpdate rows
            // (keyed by DocumentId) can be backfilled to TabId with a plain column copy below —
            // no id-mapping table required.
            migrationBuilder.Sql(
                "INSERT INTO \"DocumentTabs\" (\"Id\", \"DocumentId\", \"Title\", \"OrderIndex\", \"ContentSnapshot\", \"CreatedAt\", \"UpdatedAt\") " +
                "SELECT \"Id\", \"Id\", 'Tab 1', 0, \"ContentSnapshot\", \"CreatedAt\", \"UpdatedAt\" FROM \"Documents\"");

            migrationBuilder.AddColumn<Guid>(
                name: "TabId",
                table: "DocumentUpdates",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"DocumentUpdates\" SET \"TabId\" = \"DocumentId\"");

            migrationBuilder.AlterColumn<Guid>(
                name: "TabId",
                table: "DocumentUpdates",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TabId", table: "DocumentUpdates");
            migrationBuilder.DropTable(name: "DocumentTabs");
        }
    }
}
