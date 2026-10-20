using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Payment.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaymentDbContext))]
[Migration("202610180005_AddRefundsAndOutbox")]
public partial class AddRefundsAndOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ProviderPaymentReference", table: "Payments", type: "nvarchar(256)", maxLength: 256, nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "RefundId", table: "IdempotencyOperations", type: "uniqueidentifier", nullable: true);
        migrationBuilder.CreateTable(
            name: "Refunds",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MerchantId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(19,4)", nullable: false),
                Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                Provider = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ProviderReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Refunds", x => x.Id); table.ForeignKey("FK_Refunds_Payments_PaymentId", x => x.PaymentId, "Payments", "Id", onDelete: ReferentialAction.Restrict); });
        migrationBuilder.CreateTable(
            name: "OutcomeOutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                NextAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                DeliveredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                AttemptCount = table.Column<int>(type: "int", nullable: false),
                LastError = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_OutcomeOutboxMessages", x => x.Id));
        migrationBuilder.CreateIndex(name: "IX_IdempotencyOperations_RefundId", table: "IdempotencyOperations", column: "RefundId");
        migrationBuilder.CreateIndex(name: "IX_Refunds_MerchantId_PaymentId", table: "Refunds", columns: new[] { "MerchantId", "PaymentId" });
        migrationBuilder.CreateIndex(name: "IX_Refunds_Provider_ProviderReference", table: "Refunds", columns: new[] { "Provider", "ProviderReference" });
        migrationBuilder.CreateIndex(name: "IX_OutcomeOutboxMessages_DeliveredAt_NextAttemptAt", table: "OutcomeOutboxMessages", columns: new[] { "DeliveredAt", "NextAttemptAt" });
        migrationBuilder.AddForeignKey(name: "FK_IdempotencyOperations_Refunds_RefundId", table: "IdempotencyOperations", column: "RefundId", principalTable: "Refunds", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_IdempotencyOperations_Refunds_RefundId", "IdempotencyOperations");
        migrationBuilder.DropTable("OutcomeOutboxMessages"); migrationBuilder.DropTable("Refunds");
        migrationBuilder.DropIndex("IX_IdempotencyOperations_RefundId", "IdempotencyOperations");
        migrationBuilder.DropColumn("RefundId", "IdempotencyOperations"); migrationBuilder.DropColumn("ProviderPaymentReference", "Payments");
    }
}
