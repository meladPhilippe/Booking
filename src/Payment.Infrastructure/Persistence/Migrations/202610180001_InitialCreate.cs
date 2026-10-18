using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaymentDbContext))]
[Migration("202610180001_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Payments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MerchantId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                MerchantReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                Method = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Provider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ProviderReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Payments", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_Payments_MerchantId_MerchantReference", table: "Payments", columns: new[] { "MerchantId", "MerchantReference" });
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "Payments");
}
