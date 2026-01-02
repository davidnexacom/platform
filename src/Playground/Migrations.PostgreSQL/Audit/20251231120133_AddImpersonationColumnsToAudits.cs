using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FSH.Playground.Migrations.PostgreSQL.Audit
{
    /// <inheritdoc />
    public partial class AddImpersonationColumnsToAudits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: "audit",
                table: "AuditRecords",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<bool>(
                name: "IsImpersonating",
                schema: "audit",
                table: "AuditRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RealUserId",
                schema: "audit",
                table: "AuditRecords",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RealUserName",
                schema: "audit",
                table: "AuditRecords",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_IsImpersonating",
                schema: "audit",
                table: "AuditRecords",
                column: "IsImpersonating");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRecords_IsImpersonating_EventType",
                schema: "audit",
                table: "AuditRecords",
                columns: new[] { "IsImpersonating", "EventType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_IsImpersonating",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropIndex(
                name: "IX_AuditRecords_IsImpersonating_EventType",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropColumn(
                name: "IsImpersonating",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropColumn(
                name: "RealUserId",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.DropColumn(
                name: "RealUserName",
                schema: "audit",
                table: "AuditRecords");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                schema: "audit",
                table: "AuditRecords",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
