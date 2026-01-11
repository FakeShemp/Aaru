using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aaru.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCompressedBufferReadToTestedMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompressedBufferRead",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommandVariant = table.Column<string>(type: "TEXT", nullable: true),
                    CompressedData = table.Column<byte[]>(type: "BLOB", nullable: true),
                    UncompressedSize = table.Column<uint>(type: "INTEGER", nullable: false),
                    TestedMediaId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompressedBufferRead", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompressedBufferRead_TestedMedia_TestedMediaId",
                        column: x => x.TestedMediaId,
                        principalTable: "TestedMedia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompressedBufferRead_TestedMediaId",
                table: "CompressedBufferRead",
                column: "TestedMediaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompressedBufferRead");
        }
    }
}
