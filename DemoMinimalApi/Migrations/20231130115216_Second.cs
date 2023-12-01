using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoMinimalApi.Migrations
{
    /// <inheritdoc />
    public partial class Second : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Fornecdored",
                table: "Fornecdored");

            migrationBuilder.RenameTable(
                name: "Fornecdored",
                newName: "Fornecedores");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Fornecedores",
                table: "Fornecedores",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Fornecedores",
                table: "Fornecedores");

            migrationBuilder.RenameTable(
                name: "Fornecedores",
                newName: "Fornecdored");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Fornecdored",
                table: "Fornecdored",
                column: "Id");
        }
    }
}
