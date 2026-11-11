using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMZN.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUploadUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageUploadUsage",
                schema: "AMZN",
                columns: table => new
                {
                    Month = table.Column<int>(type: "int", nullable: false),
                    ReservedBytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageUploadUsage", x => x.Month);
                    table.CheckConstraint("CK_ImageUploadUsage_ReservedBytes", "ReservedBytes >= 0");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageUploadUsage",
                schema: "AMZN");
        }
    }
}
