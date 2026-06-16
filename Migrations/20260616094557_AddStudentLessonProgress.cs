using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POT_System_ASPNET.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentLessonProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudentLessonProgress",
                columns: table => new
                {
                    progress_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    lesson_id = table.Column<int>(type: "int", nullable: false),
                    completed_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentLessonProgress", x => x.progress_id);
                    table.ForeignKey(
                        name: "FK_StudentLessonProgress_Lesson_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "Lesson",
                        principalColumn: "lesson_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentLessonProgress_User_student_id",
                        column: x => x.student_id,
                        principalTable: "User",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentLessonProgress_lesson_id",
                table: "StudentLessonProgress",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLessonProgress_student_id",
                table: "StudentLessonProgress",
                column: "student_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentLessonProgress");
        }
    }
}
