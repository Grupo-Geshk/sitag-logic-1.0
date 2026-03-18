using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SITAG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "tenants",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryEmail",
                table: "tenants",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "users");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "PrimaryEmail",
                table: "tenants");
        }
    }
}
