using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SITAG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProducerModuleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FarmType",
                table: "farms",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOwned",
                table: "farms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "economy_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "economy_transactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxCapacity",
                table: "divisions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                table: "animals",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Breed",
                table: "animals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "animals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                table: "animals",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "animals",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FarmType",
                table: "farms");

            migrationBuilder.DropColumn(
                name: "IsOwned",
                table: "farms");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "economy_transactions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "economy_transactions");

            migrationBuilder.DropColumn(
                name: "MaxCapacity",
                table: "divisions");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "animals");

            migrationBuilder.DropColumn(
                name: "Breed",
                table: "animals");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "animals");

            migrationBuilder.DropColumn(
                name: "Sex",
                table: "animals");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "animals");
        }
    }
}
