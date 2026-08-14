using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Order.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPriorStatusToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "PriorStatus",
                table: "Orders",
                type: "smallint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriorStatus",
                table: "Orders");
        }
    }
}
