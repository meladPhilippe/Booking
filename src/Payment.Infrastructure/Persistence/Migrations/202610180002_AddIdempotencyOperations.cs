using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Payment.Infrastructure.Persistence;

#nullable disable

namespace Payment.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaymentDbContext))]
[Migration("202610180002_AddIdempotencyOperations")]
public partial class AddIdempotencyOperations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "IdempotencyOperations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MerchantId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Operation = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                FingerprintVersion = table.Column<int>(type: "int", nullable: false),
                Fingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IdempotencyOperations", x => x.Id);
                table.ForeignKey("FK_IdempotencyOperations_Payments_PaymentId", x => x.PaymentId, "Payments", "Id", onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex("IX_IdempotencyOperations_MerchantId_Operation_IdempotencyKey", "IdempotencyOperations", new[] { "MerchantId", "Operation", "IdempotencyKey" }, unique: true);
        migrationBuilder.CreateIndex("IX_IdempotencyOperations_MerchantId_PaymentId", "IdempotencyOperations", new[] { "MerchantId", "PaymentId" });
        migrationBuilder.CreateIndex("IX_IdempotencyOperations_PaymentId", "IdempotencyOperations", "PaymentId", unique: true);
        migrationBuilder.CreateIndex("IX_Payments_MerchantId_Id", "Payments", new[] { "MerchantId", "Id" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("IdempotencyOperations");
        migrationBuilder.DropIndex("IX_Payments_MerchantId_Id", "Payments");
    }
}
