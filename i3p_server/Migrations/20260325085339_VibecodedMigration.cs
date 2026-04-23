using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace i3p_server.Migrations
{
    /// <inheritdoc />
    public partial class VibecodedMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppItems_AnnualProcurementPlan_AnnualProcurementPlanId",
                table: "AppItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanCrossReferences_AppItems_AppItemId",
                table: "PlanCrossReferences");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcurementItemBs_ProcurementPlanBs_ProcurementPlanBId",
                table: "ProcurementItemBs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcurementItemBs",
                table: "ProcurementItemBs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AnnualProcurementPlan",
                table: "AnnualProcurementPlan");

            migrationBuilder.DropColumn(
                name: "IsPhotoVerified",
                table: "AppItems");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "AppItems");

            migrationBuilder.DropColumn(
                name: "YearTotal",
                table: "AnnualProcurementPlan");

            migrationBuilder.RenameTable(
                name: "ProcurementItemBs",
                newName: "ProcurementItemB");

            migrationBuilder.RenameTable(
                name: "AnnualProcurementPlan",
                newName: "AnnualProcurementPlans");

            migrationBuilder.RenameIndex(
                name: "IX_PlanCrossReferences_AppItemId",
                table: "PlanCrossReferences",
                newName: "IX_PlanCrossReference_AppItemId");

            migrationBuilder.RenameColumn(
                name: "VerifiedBy",
                table: "AppItems",
                newName: "Remarks");

            migrationBuilder.RenameColumn(
                name: "PhotoPath",
                table: "AppItems",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "ArCode",
                table: "AppItems",
                newName: "AccountCode");

            migrationBuilder.RenameIndex(
                name: "IX_ProcurementItemBs_ProcurementPlanBId",
                table: "ProcurementItemB",
                newName: "IX_ProcurementItemB_ProcurementPlanBId");

            migrationBuilder.RenameColumn(
                name: "HeadersJson",
                table: "AnnualProcurementPlans",
                newName: "Title");

            migrationBuilder.AddColumn<string>(
                name: "ArCode",
                table: "PlanCrossReferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "PlanCrossReferences",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "PlanCrossReferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PlanCrossReferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "PlanCrossReferences",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "TotalCost",
                table: "PlanCrossReferences",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "UnitCost",
                table: "PlanCrossReferences",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "AnnualProcurementPlans",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AnnualProcurementPlans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalBudget",
                table: "AnnualProcurementPlans",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcurementItemB",
                table: "ProcurementItemB",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AnnualProcurementPlans",
                table: "AnnualProcurementPlans",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlanCrossReference_ArCode",
                table: "PlanCrossReferences",
                column: "ArCode");

            migrationBuilder.CreateIndex(
                name: "IX_PlanCrossReference_Year_Status",
                table: "PlanCrossReferences",
                columns: new[] { "Year", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_AppItems_AnnualProcurementPlans_AnnualProcurementPlanId",
                table: "AppItems",
                column: "AnnualProcurementPlanId",
                principalTable: "AnnualProcurementPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcurementItemB_ProcurementPlanBs_ProcurementPlanBId",
                table: "ProcurementItemB",
                column: "ProcurementPlanBId",
                principalTable: "ProcurementPlanBs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppItems_AnnualProcurementPlans_AnnualProcurementPlanId",
                table: "AppItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcurementItemB_ProcurementPlanBs_ProcurementPlanBId",
                table: "ProcurementItemB");

            migrationBuilder.DropIndex(
                name: "IX_PlanCrossReference_ArCode",
                table: "PlanCrossReferences");

            migrationBuilder.DropIndex(
                name: "IX_PlanCrossReference_Year_Status",
                table: "PlanCrossReferences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProcurementItemB",
                table: "ProcurementItemB");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AnnualProcurementPlans",
                table: "AnnualProcurementPlans");

            migrationBuilder.DropColumn(
                name: "ArCode",
                table: "PlanCrossReferences");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "PlanCrossReferences");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "PlanCrossReferences");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PlanCrossReferences");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "PlanCrossReferences");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "PlanCrossReferences");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "PlanCrossReferences");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "AnnualProcurementPlans");

            migrationBuilder.DropColumn(
                name: "TotalBudget",
                table: "AnnualProcurementPlans");

            migrationBuilder.RenameTable(
                name: "ProcurementItemB",
                newName: "ProcurementItemBs");

            migrationBuilder.RenameTable(
                name: "AnnualProcurementPlans",
                newName: "AnnualProcurementPlan");

            migrationBuilder.RenameIndex(
                name: "IX_PlanCrossReference_AppItemId",
                table: "PlanCrossReferences",
                newName: "IX_PlanCrossReferences_AppItemId");

            migrationBuilder.RenameColumn(
                name: "Remarks",
                table: "AppItems",
                newName: "VerifiedBy");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "AppItems",
                newName: "PhotoPath");

            migrationBuilder.RenameColumn(
                name: "AccountCode",
                table: "AppItems",
                newName: "ArCode");

            migrationBuilder.RenameIndex(
                name: "IX_ProcurementItemB_ProcurementPlanBId",
                table: "ProcurementItemBs",
                newName: "IX_ProcurementItemBs_ProcurementPlanBId");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "AnnualProcurementPlan",
                newName: "HeadersJson");

            migrationBuilder.AddColumn<bool>(
                name: "IsPhotoVerified",
                table: "AppItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "AppItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "AnnualProcurementPlan",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "YearTotal",
                table: "AnnualProcurementPlan",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProcurementItemBs",
                table: "ProcurementItemBs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AnnualProcurementPlan",
                table: "AnnualProcurementPlan",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AppItems_AnnualProcurementPlan_AnnualProcurementPlanId",
                table: "AppItems",
                column: "AnnualProcurementPlanId",
                principalTable: "AnnualProcurementPlan",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanCrossReferences_AppItems_AppItemId",
                table: "PlanCrossReferences",
                column: "AppItemId",
                principalTable: "AppItems",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProcurementItemBs_ProcurementPlanBs_ProcurementPlanBId",
                table: "ProcurementItemBs",
                column: "ProcurementPlanBId",
                principalTable: "ProcurementPlanBs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
