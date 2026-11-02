using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMZN.Migrations
{
    /// <inheritdoc />
    public partial class TrackAppliedActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApplied",
                schema: "AMZN",
                table: "Actions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE [AMZN].[Actions] SET [IsApplied] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApplied",
                schema: "AMZN",
                table: "Actions");
        }
    }
}
