using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaymentDbContext))]
[Migration("202610200001_NormalizeProviderKeys")]
public partial class NormalizeProviderKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE Payments SET Provider = LOWER(Provider)");
        migrationBuilder.Sql("UPDATE Refunds SET Provider = LOWER(Provider)");
        migrationBuilder.Sql("UPDATE ProviderWebhookEvents SET Provider = LOWER(Provider)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
