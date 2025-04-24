using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aaru.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLiteOnRawReadDvdToReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "LiteOnReadRawDVDData",
                table: "TestedMedia",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SupportsLiteOnReadRawDVD",
                table: "TestedMedia",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LiteOnReadRawDVDData",
                table: "TestedMedia");

            migrationBuilder.DropColumn(
                name: "SupportsLiteOnReadRawDVD",
                table: "TestedMedia");
        }
    }
}
