using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Customer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeEmailGloballyUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Customers_Email_Active",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "UX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Customers_Email",
                table: "Customers");

            migrationBuilder.CreateIndex(
                name: "UX_Customers_Email_Active",
                table: "Customers",
                column: "Email",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
