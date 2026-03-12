using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace i3p_server.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnnualProcurementPlan",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    YearTotal = table.Column<decimal>(type: "numeric", nullable: false),
                    AuxilliaryJson = table.Column<string>(type: "text", nullable: true),
                    HeadersJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnnualProcurementPlan", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenditureData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SheetName = table.Column<string>(type: "text", nullable: false),
                    AuxilliaryJson = table.Column<string>(type: "text", nullable: true),
                    HeadersJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenditureData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PPMPs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SheetName = table.Column<string>(type: "text", nullable: false),
                    AuxilliaryJson = table.Column<string>(type: "text", nullable: true),
                    HeadersJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PPMPs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcurementPlanBs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SheetName = table.Column<string>(type: "text", nullable: false),
                    AuxilliaryJson = table.Column<string>(type: "text", nullable: true),
                    HeadersJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcurementPlanBs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolImplementations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    SheetName = table.Column<string>(type: "text", nullable: false),
                    TotalEstimatedCost = table.Column<double>(type: "double precision", nullable: false),
                    AuxilliaryJson = table.Column<string>(type: "text", nullable: true),
                    HeadersJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolImplementations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    authority = table.Column<int>(type: "integer", nullable: false),
                    photo_url = table.Column<string>(type: "text", nullable: true),
                    date_created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AppItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnnualProcurementPlanId = table.Column<int>(type: "integer", nullable: false),
                    No = table.Column<string>(type: "text", nullable: true),
                    Unspsc = table.Column<string>(type: "text", nullable: true),
                    ItemDescription = table.Column<string>(type: "text", nullable: true),
                    Specification = table.Column<string>(type: "text", nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "text", nullable: true),
                    TotalQuantity = table.Column<double>(type: "double precision", nullable: true),
                    Price = table.Column<double>(type: "double precision", nullable: true),
                    TotalAmount = table.Column<double>(type: "double precision", nullable: true),
                    ArCode = table.Column<string>(type: "text", nullable: true),
                    PhotoPath = table.Column<string>(type: "text", nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsPhotoVerified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppItems_AnnualProcurementPlan_AnnualProcurementPlanId",
                        column: x => x.AnnualProcurementPlanId,
                        principalTable: "AnnualProcurementPlan",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpenditureItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExpenditureDataId = table.Column<int>(type: "integer", nullable: false),
                    SpecificProgram = table.Column<string>(type: "text", nullable: true),
                    Output = table.Column<string>(type: "text", nullable: true),
                    Activities = table.Column<string>(type: "text", nullable: true),
                    PerformanceIndicator = table.Column<string>(type: "text", nullable: true),
                    ExpenseClass = table.Column<string>(type: "text", nullable: true),
                    ExpenseObject = table.Column<string>(type: "text", nullable: true),
                    ExpenseItem = table.Column<string>(type: "text", nullable: true),
                    UnitCost = table.Column<double>(type: "double precision", nullable: true),
                    Quantity = table.Column<double>(type: "double precision", nullable: true),
                    TotalCost = table.Column<double>(type: "double precision", nullable: true),
                    IsPpmp = table.Column<string>(type: "text", nullable: true),
                    IsAppSupplies = table.Column<string>(type: "text", nullable: true),
                    MannerOfRelease = table.Column<string>(type: "text", nullable: true),
                    PhysicalTarget2026 = table.Column<string>(type: "text", nullable: true),
                    FinancialObligation = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenditureItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenditureItem_ExpenditureData_ExpenditureDataId",
                        column: x => x.ExpenditureDataId,
                        principalTable: "ExpenditureData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PpmpItem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PPMPId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: true),
                    GeneralDescription = table.Column<string>(type: "text", nullable: true),
                    Units = table.Column<string>(type: "text", nullable: true),
                    UnitPrice = table.Column<double>(type: "double precision", nullable: true),
                    Quantity = table.Column<double>(type: "double precision", nullable: true),
                    EstimatedBudget = table.Column<double>(type: "double precision", nullable: true),
                    ModeOfProcurement = table.Column<string>(type: "text", nullable: true),
                    ScheduleJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PpmpItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PpmpItem_PPMPs_PPMPId",
                        column: x => x.PPMPId,
                        principalTable: "PPMPs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProcurementItemBs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProcurementPlanBId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: true),
                    CategoryId = table.Column<string>(type: "text", nullable: true),
                    Code = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Unit = table.Column<string>(type: "text", nullable: true),
                    UnitPrice = table.Column<double>(type: "double precision", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcurementItemBs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcurementItemBs_ProcurementPlanBs_ProcurementPlanBId",
                        column: x => x.ProcurementPlanBId,
                        principalTable: "ProcurementPlanBs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImplementationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SchoolImplementationId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<string>(type: "text", nullable: true),
                    Kra = table.Column<string>(type: "text", nullable: true),
                    SipProgram = table.Column<string>(type: "text", nullable: true),
                    Activity = table.Column<string>(type: "text", nullable: true),
                    Purpose = table.Column<string>(type: "text", nullable: true),
                    Indicator = table.Column<string>(type: "text", nullable: true),
                    Resources = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<string>(type: "text", nullable: true),
                    EstimatedCost = table.Column<double>(type: "double precision", nullable: true),
                    AccountTitle = table.Column<string>(type: "text", nullable: true),
                    AccountCode = table.Column<string>(type: "text", nullable: true),
                    ExpenditureType = table.Column<string>(type: "text", nullable: true),
                    ArCode = table.Column<string>(type: "text", nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImplementationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImplementationItems_SchoolImplementations_SchoolImplementat~",
                        column: x => x.SchoolImplementationId,
                        principalTable: "SchoolImplementations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanCrossReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    AppItemId = table.Column<int>(type: "integer", nullable: true),
                    AppItemPrice = table.Column<double>(type: "double precision", nullable: false),
                    AppItemDescription = table.Column<string>(type: "text", nullable: true),
                    ImplementationItemId = table.Column<int>(type: "integer", nullable: true),
                    SipItemCost = table.Column<double>(type: "double precision", nullable: true),
                    SipItemActivity = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsOrphaned = table.Column<bool>(type: "boolean", nullable: false),
                    MatchScore = table.Column<double>(type: "double precision", nullable: false),
                    AdminNote = table.Column<string>(type: "text", nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanCrossReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanCrossReferences_AppItems_AppItemId",
                        column: x => x.AppItemId,
                        principalTable: "AppItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlanCrossReferences_ImplementationItems_ImplementationItemId",
                        column: x => x.ImplementationItemId,
                        principalTable: "ImplementationItems",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppItems_AnnualProcurementPlanId",
                table: "AppItems",
                column: "AnnualProcurementPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenditureItem_ExpenditureDataId",
                table: "ExpenditureItem",
                column: "ExpenditureDataId");

            migrationBuilder.CreateIndex(
                name: "IX_ImplementationItems_SchoolImplementationId",
                table: "ImplementationItems",
                column: "SchoolImplementationId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanCrossReferences_AppItemId",
                table: "PlanCrossReferences",
                column: "AppItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanCrossReferences_ImplementationItemId",
                table: "PlanCrossReferences",
                column: "ImplementationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PpmpItem_PPMPId",
                table: "PpmpItem",
                column: "PPMPId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementItemBs_ProcurementPlanBId",
                table: "ProcurementItemBs",
                column: "ProcurementPlanBId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenditureItem");

            migrationBuilder.DropTable(
                name: "PlanCrossReferences");

            migrationBuilder.DropTable(
                name: "PpmpItem");

            migrationBuilder.DropTable(
                name: "ProcurementItemBs");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "ExpenditureData");

            migrationBuilder.DropTable(
                name: "AppItems");

            migrationBuilder.DropTable(
                name: "ImplementationItems");

            migrationBuilder.DropTable(
                name: "PPMPs");

            migrationBuilder.DropTable(
                name: "ProcurementPlanBs");

            migrationBuilder.DropTable(
                name: "AnnualProcurementPlan");

            migrationBuilder.DropTable(
                name: "SchoolImplementations");
        }
    }
}
