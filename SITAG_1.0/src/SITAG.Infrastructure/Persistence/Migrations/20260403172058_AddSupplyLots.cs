using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SITAG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupplyLots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_animals_TenantId_TagNumber",
                table: "animals");

            migrationBuilder.AddColumn<Guid>(
                name: "LotId",
                table: "supply_movements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TagNumber",
                table: "animals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateTable(
                name: "supply_lots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyId = table.Column<Guid>(type: "uuid", nullable: false),
                    InitialQuantity = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    CurrentQuantity = table.Column<decimal>(type: "numeric(12,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(14,2)", nullable: true),
                    Supplier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ExpirationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supply_lots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_supply_lots_supplies_SupplyId",
                        column: x => x.SupplyId,
                        principalTable: "supplies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // worker_loans table already exists in production (created via 20260402080000_AddWorkerLoans)

            migrationBuilder.CreateIndex(
                name: "IX_supply_movements_LotId",
                table: "supply_movements",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_animals_TenantId_TagNumber",
                table: "animals",
                columns: new[] { "TenantId", "TagNumber" },
                unique: true,
                filter: "\"TagNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_supply_lots_SupplyId",
                table: "supply_lots",
                column: "SupplyId");

            // IX_worker_loans_WorkerId_LoanDate already exists in production

            migrationBuilder.AddForeignKey(
                name: "FK_supply_movements_supply_lots_LotId",
                table: "supply_movements",
                column: "LotId",
                principalTable: "supply_lots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_supply_movements_supply_lots_LotId",
                table: "supply_movements");

            migrationBuilder.DropTable(
                name: "supply_lots");

            // worker_loans intentionally kept — managed by 20260402080000_AddWorkerLoans

            migrationBuilder.DropIndex(
                name: "IX_supply_movements_LotId",
                table: "supply_movements");

            migrationBuilder.DropIndex(
                name: "IX_animals_TenantId_TagNumber",
                table: "animals");

            migrationBuilder.DropColumn(
                name: "LotId",
                table: "supply_movements");

            migrationBuilder.AlterColumn<string>(
                name: "TagNumber",
                table: "animals",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_animals_TenantId_TagNumber",
                table: "animals",
                columns: new[] { "TenantId", "TagNumber" },
                unique: true);
        }
    }
}
