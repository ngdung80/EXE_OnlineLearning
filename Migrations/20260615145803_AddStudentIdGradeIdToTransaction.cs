using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POT_System_ASPNET.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentIdGradeIdToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "grade_id",
                table: "Transaction",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "student_id",
                table: "Transaction",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "grade_id",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "student_id",
                table: "Transaction");
        }
    }
}
