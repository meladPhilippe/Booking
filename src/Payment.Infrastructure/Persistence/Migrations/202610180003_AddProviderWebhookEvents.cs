using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Payment.Infrastructure.Persistence;

#nullable disable

namespace Payment.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaymentDbContext))]
[Migration("202610180003_AddProviderWebhookEvents")]
public partial class AddProviderWebhookEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ProviderWebhookEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Provider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ExternalEventId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                RelatedObjectId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_ProviderWebhookEvents", x => x.Id));
        migrationBuilder.CreateIndex("IX_ProviderWebhookEvents_Provider_ExternalEventId", "ProviderWebhookEvents", new[] { "Provider", "ExternalEventId" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable("ProviderWebhookEvents");
}
