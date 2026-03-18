using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SITAG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeSupplyFarmIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "FarmId",
                table: "supplies",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "worker_payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FarmId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_worker_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_worker_payments_workers_WorkerId",
                        column: x => x.WorkerId,
                        principalTable: "workers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_worker_payments_WorkerId_PaymentDate",
                table: "worker_payments",
                columns: new[] { "WorkerId", "PaymentDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "worker_payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "FarmId",
                table: "supplies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
