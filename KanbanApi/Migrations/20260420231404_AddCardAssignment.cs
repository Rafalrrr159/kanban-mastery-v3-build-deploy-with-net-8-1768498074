using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KanbanApi.Migrations
{
    /// <inheritdoc />
    public partial class AddCardAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedToUserId",
                table: "Cards",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_AssignedToUserId",
                table: "Cards",
                column: "AssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_AspNetUsers_AssignedToUserId",
                table: "Cards",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_AspNetUsers_AssignedToUserId",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_Cards_AssignedToUserId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Cards");
        }
    }
}
