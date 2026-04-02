using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SITAG.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordResetTokenHash",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PasswordResetTokenExpiresAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE users DROP COLUMN IF EXISTS ""PasswordResetTokenHash""");
            migrationBuilder.Sql(@"ALTER TABLE users DROP COLUMN IF EXISTS ""PasswordResetTokenExpiresAt""");
        }
    }
}
