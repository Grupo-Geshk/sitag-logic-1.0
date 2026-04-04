using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SITAG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeHierrosTenantLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_farm_brands_farms_FarmId",
                table: "farm_brands");

            migrationBuilder.DropIndex(
                name: "IX_farm_brands_FarmId",
                table: "farm_brands");

            migrationBuilder.DropColumn(
                name: "FarmId",
                table: "farm_brands");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FarmId",
                table: "farm_brands",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_farm_brands_FarmId",
                table: "farm_brands",
                column: "FarmId");

            migrationBuilder.AddForeignKey(
                name: "FK_farm_brands_farms_FarmId",
                table: "farm_brands",
                column: "FarmId",
                principalTable: "farms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
