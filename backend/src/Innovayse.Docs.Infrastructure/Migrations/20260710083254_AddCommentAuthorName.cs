using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innovayse.Docs.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentAuthorName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "Comments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "Comments");
        }
    }
}
