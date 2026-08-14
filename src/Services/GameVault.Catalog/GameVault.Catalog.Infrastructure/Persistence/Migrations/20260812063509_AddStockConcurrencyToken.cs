using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameVault.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin is a PostgreSQL system column that exists on every table row automatically.
            // No DDL is needed — this migration only updates the EF model snapshot so that
            // EF Core reads xmin as an optimistic-concurrency token on Stock rows.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo — xmin is a system column, not a user-created column.
        }
    }
}
