using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace i3p_server.Migrations
{
    /// <inheritdoc />
    public partial class MakeDetailOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expense_records_expense_details_detail_id",
                table: "expense_records");

            migrationBuilder.AlterColumn<int>(
                name: "detail_id",
                table: "expense_records",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_expense_records_expense_details_detail_id",
                table: "expense_records",
                column: "detail_id",
                principalTable: "expense_details",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expense_records_expense_details_detail_id",
                table: "expense_records");

            migrationBuilder.AlterColumn<int>(
                name: "detail_id",
                table: "expense_records",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_expense_records_expense_details_detail_id",
                table: "expense_records",
                column: "detail_id",
                principalTable: "expense_details",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
