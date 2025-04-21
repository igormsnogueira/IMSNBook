using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMSNBook.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class fixPhoneNumberNameOnCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PHoneNumber",
                table: "Companies",
                newName: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "Companies",
                newName: "PHoneNumber");
        }
    }
}
