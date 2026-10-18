using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Payment.Infrastructure.Persistence;

#nullable disable

namespace Payment.Infrastructure.Persistence.Migrations;

[DbContext(typeof(PaymentDbContext))]
[Migration("202610180004_AllowCaptureIdempotency")]
public partial class AllowCaptureIdempotency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_IdempotencyOperations_PaymentId", "IdempotencyOperations");
        migrationBuilder.CreateIndex("IX_IdempotencyOperations_PaymentId", "IdempotencyOperations", "PaymentId");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_IdempotencyOperations_PaymentId", "IdempotencyOperations");
        migrationBuilder.CreateIndex("IX_IdempotencyOperations_PaymentId", "IdempotencyOperations", "PaymentId", unique: true);
    }
}
