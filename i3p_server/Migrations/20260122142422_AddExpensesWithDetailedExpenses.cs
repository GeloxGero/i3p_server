using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace i3p_server.Migrations
{
    /// <inheritdoc />
    public partial class AddExpensesWithDetailedExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expense_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    technical_specification = table.Column<string>(type: "text", nullable: true),
                    vendor_name = table.Column<string>(type: "text", nullable: true),
                    justification = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_details", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "expense_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    expense_class = table.Column<string>(type: "text", nullable: false),
                    dbm_grouping = table.Column<string>(type: "text", nullable: false),
                    expense_item = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    manner_of_release = table.Column<string>(type: "text", nullable: false),
                    detail_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_expense_records_expense_details_detail_id",
                        column: x => x.detail_id,
                        principalTable: "expense_details",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_expense_records_detail_id",
                table: "expense_records",
                column: "detail_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expense_records");

            migrationBuilder.DropTable(
                name: "expense_details");
        }
    }
}
