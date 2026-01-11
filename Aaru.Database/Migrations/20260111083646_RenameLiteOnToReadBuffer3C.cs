using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aaru.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameLiteOnToReadBuffer3C : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SupportsLiteOnReadRawDVD",
                table: "TestedMedia",
                newName: "SupportsReadBuffer3CRawDVD");

            migrationBuilder.RenameColumn(
                name: "LiteOnReadRawDVDData",
                table: "TestedMedia",
                newName: "ReadBuffer3CRawDVDData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SupportsReadBuffer3CRawDVD",
                table: "TestedMedia",
                newName: "SupportsLiteOnReadRawDVD");

            migrationBuilder.RenameColumn(
                name: "ReadBuffer3CRawDVDData",
                table: "TestedMedia",
                newName: "LiteOnReadRawDVDData");
        }
    }
}
