using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace i3p_server.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnualBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AnnualBudget",
                table: "SchoolImplementations",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnualBudget",
                table: "SchoolImplementations");
        }
    }
}
