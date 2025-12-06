using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mini_Project_Assignment_Y2S2.Migrations
{
    /// <inheritdoc />
    public partial class DBTan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Location");

            migrationBuilder.DropColumn(
                name: "ContactInfo",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Items",
                newName: "Image");

            migrationBuilder.RenameColumn(
                name: "LostDate",
                table: "Items",
                newName: "Date");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Items",
                newName: "ItemID");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IName",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IType",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Idescription",
                table: "Items",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IName",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "IType",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Idescription",
                table: "Items");

            migrationBuilder.RenameColumn(
                name: "Image",
                table: "Items",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Items",
                newName: "LostDate");

            migrationBuilder.RenameColumn(
                name: "ItemID",
                table: "Items",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "ContactInfo",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Items",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Location",
                columns: table => new
                {
                    ID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Location", x => x.ID);
                });
        }
    }
}
