using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace i3p_server.Migrations
{
    /// <inheritdoc />
    public partial class ExpensesDatabaseEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expense_records");

            migrationBuilder.DropTable(
                name: "expense_details");

            migrationBuilder.CreateTable(
                name: "expense_summaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    expense_class = table.Column<string>(type: "text", nullable: false),
                    dbm_grouping = table.Column<string>(type: "text", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    manner_of_release = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_summaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "procurement_details",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    summary_id = table.Column<int>(type: "integer", nullable: false),
                    item_description = table.Column<string>(type: "text", nullable: false),
                    unit_measure = table.Column<string>(type: "text", nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric", nullable: false),
                    total_qty = table.Column<double>(type: "double precision", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_procurement_details", x => x.Id);
                    table.ForeignKey(
                        name: "FK_procurement_details_expense_summaries_summary_id",
                        column: x => x.summary_id,
                        principalTable: "expense_summaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_procurement_details_summary_id",
                table: "procurement_details",
                column: "summary_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "procurement_details");

            migrationBuilder.DropTable(
                name: "expense_summaries");

            migrationBuilder.CreateTable(
                name: "expense_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    justification = table.Column<string>(type: "text", nullable: true),
                    technical_specification = table.Column<string>(type: "text", nullable: true),
                    vendor_name = table.Column<string>(type: "text", nullable: true)
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
                    detail_id = table.Column<int>(type: "integer", nullable: true),
                    dbm_grouping = table.Column<string>(type: "text", nullable: false),
                    expense_class = table.Column<string>(type: "text", nullable: false),
                    expense_item = table.Column<string>(type: "text", nullable: false),
                    manner_of_release = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_expense_records_expense_details_detail_id",
                        column: x => x.detail_id,
                        principalTable: "expense_details",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_expense_records_detail_id",
                table: "expense_records",
                column: "detail_id");
        }
    }
}
