using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POT_System_ASPNET.Migrations
{
    /// <inheritdoc />
    public partial class AddVocabularyJsonToLesson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "vocabulary_json",
                table: "Lesson",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "vocabulary_json",
                table: "Lesson");
        }
    }
}
