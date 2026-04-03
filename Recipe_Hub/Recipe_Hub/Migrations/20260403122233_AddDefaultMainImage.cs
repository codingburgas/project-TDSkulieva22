using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Recipe_Hub.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultMainImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MainImagePath",
                table: "Recipes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "/Resources/Images/default.png",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MainImagePath",
                table: "Recipes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "/Resources/Images/default.png");
        }
    }
}
