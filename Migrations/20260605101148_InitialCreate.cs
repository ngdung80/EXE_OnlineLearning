using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POT_System_ASPNET.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Grade",
                columns: table => new
                {
                    grade_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    grade_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grade", x => x.grade_id);
                });

            migrationBuilder.CreateTable(
                name: "Package",
                columns: table => new
                {
                    package_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    package_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    price = table.Column<double>(type: "float", nullable: false),
                    duration = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Package", x => x.package_id);
                });

            migrationBuilder.CreateTable(
                name: "Subject",
                columns: table => new
                {
                    subject_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    grade_id = table.Column<int>(type: "int", nullable: false),
                    subject_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subject", x => x.subject_id);
                    table.ForeignKey(
                        name: "FK_Subject_Grade_grade_id",
                        column: x => x.grade_id,
                        principalTable: "Grade",
                        principalColumn: "grade_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    grade_id = table.Column<int>(type: "int", nullable: true),
                    parent_id = table.Column<int>(type: "int", nullable: true),
                    full_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    dob = table.Column<DateOnly>(type: "date", nullable: true),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    specialization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    work_time = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    image = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_User_Grade_grade_id",
                        column: x => x.grade_id,
                        principalTable: "Grade",
                        principalColumn: "grade_id");
                    table.ForeignKey(
                        name: "FK_User_User_parent_id",
                        column: x => x.parent_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Chapter",
                columns: table => new
                {
                    chapter_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    subject_id = table.Column<int>(type: "int", nullable: false),
                    chapter_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapter", x => x.chapter_id);
                    table.ForeignKey(
                        name: "FK_Chapter_Subject_subject_id",
                        column: x => x.subject_id,
                        principalTable: "Subject",
                        principalColumn: "subject_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MentorAssignment",
                columns: table => new
                {
                    assignment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mentor_id = table.Column<int>(type: "int", nullable: false),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    assigned_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentorAssignment", x => x.assignment_id);
                    table.ForeignKey(
                        name: "FK_MentorAssignment_User_mentor_id",
                        column: x => x.mentor_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_MentorAssignment_User_student_id",
                        column: x => x.student_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    notification_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.notification_id);
                    table.ForeignKey(
                        name: "FK_Notification_User_user_id",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "StudentPackage",
                columns: table => new
                {
                    student_package_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    package_id = table.Column<int>(type: "int", nullable: false),
                    grade_id = table.Column<int>(type: "int", nullable: true),
                    subject_id = table.Column<int>(type: "int", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentPackage", x => x.student_package_id);
                    table.ForeignKey(
                        name: "FK_StudentPackage_Grade_grade_id",
                        column: x => x.grade_id,
                        principalTable: "Grade",
                        principalColumn: "grade_id");
                    table.ForeignKey(
                        name: "FK_StudentPackage_Package_package_id",
                        column: x => x.package_id,
                        principalTable: "Package",
                        principalColumn: "package_id");
                    table.ForeignKey(
                        name: "FK_StudentPackage_Subject_subject_id",
                        column: x => x.subject_id,
                        principalTable: "Subject",
                        principalColumn: "subject_id");
                    table.ForeignKey(
                        name: "FK_StudentPackage_User_student_id",
                        column: x => x.student_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    transaction_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    package_id = table.Column<int>(type: "int", nullable: false),
                    student_package_id = table.Column<int>(type: "int", nullable: true),
                    mentee_count = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<double>(type: "float", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction", x => x.transaction_id);
                    table.ForeignKey(
                        name: "FK_Transaction_Package_package_id",
                        column: x => x.package_id,
                        principalTable: "Package",
                        principalColumn: "package_id");
                    table.ForeignKey(
                        name: "FK_Transaction_User_user_id",
                        column: x => x.user_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Wallet",
                columns: table => new
                {
                    wallet_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    parent_id = table.Column<int>(type: "int", nullable: false),
                    balance = table.Column<double>(type: "float", nullable: false),
                    last_updated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallet", x => x.wallet_id);
                    table.ForeignKey(
                        name: "FK_Wallet_User_parent_id",
                        column: x => x.parent_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "Lesson",
                columns: table => new
                {
                    lesson_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    lesson_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    chapter_id = table.Column<int>(type: "int", nullable: false),
                    content_text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    file_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    inactive_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    image_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    deleted_date = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lesson", x => x.lesson_id);
                    table.ForeignKey(
                        name: "FK_Lesson_Chapter_chapter_id",
                        column: x => x.chapter_id,
                        principalTable: "Chapter",
                        principalColumn: "chapter_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransaction",
                columns: table => new
                {
                    wallet_transaction_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    wallet_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<double>(type: "float", nullable: false),
                    transaction_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    package_id = table.Column<int>(type: "int", nullable: true),
                    student_package_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransaction", x => x.wallet_transaction_id);
                    table.ForeignKey(
                        name: "FK_WalletTransaction_Package_package_id",
                        column: x => x.package_id,
                        principalTable: "Package",
                        principalColumn: "package_id");
                    table.ForeignKey(
                        name: "FK_WalletTransaction_Wallet_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "Wallet",
                        principalColumn: "wallet_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Question",
                columns: table => new
                {
                    question_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    question_content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    lesson_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_multiple_choice = table.Column<bool>(type: "bit", nullable: false),
                    answer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    correct_answer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    level = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Question", x => x.question_id);
                    table.ForeignKey(
                        name: "FK_Question_Lesson_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "Lesson",
                        principalColumn: "lesson_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Test",
                columns: table => new
                {
                    test_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    test_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    subject_id = table.Column<int>(type: "int", nullable: true),
                    lesson_id = table.Column<int>(type: "int", nullable: true),
                    duration = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    types = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    student_id = table.Column<int>(type: "int", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Test", x => x.test_id);
                    table.ForeignKey(
                        name: "FK_Test_Lesson_lesson_id",
                        column: x => x.lesson_id,
                        principalTable: "Lesson",
                        principalColumn: "lesson_id");
                    table.ForeignKey(
                        name: "FK_Test_Subject_subject_id",
                        column: x => x.subject_id,
                        principalTable: "Subject",
                        principalColumn: "subject_id");
                    table.ForeignKey(
                        name: "FK_Test_User_student_id",
                        column: x => x.student_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "QuestionReport",
                columns: table => new
                {
                    report_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    question_id = table.Column<int>(type: "int", nullable: false),
                    reported_by = table.Column<int>(type: "int", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    review_note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    resolution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    reviewed_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionReport", x => x.report_id);
                    table.ForeignKey(
                        name: "FK_QuestionReport_Question_question_id",
                        column: x => x.question_id,
                        principalTable: "Question",
                        principalColumn: "question_id");
                    table.ForeignKey(
                        name: "FK_QuestionReport_User_reported_by",
                        column: x => x.reported_by,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "TestAttempt",
                columns: table => new
                {
                    attempt_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    test_id = table.Column<int>(type: "int", nullable: false),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    start_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    end_time = table.Column<DateTime>(type: "datetime2", nullable: true),
                    score = table.Column<double>(type: "float", nullable: true),
                    total_questions = table.Column<int>(type: "int", nullable: true),
                    correct_answers = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestAttempt", x => x.attempt_id);
                    table.ForeignKey(
                        name: "FK_TestAttempt_Test_test_id",
                        column: x => x.test_id,
                        principalTable: "Test",
                        principalColumn: "test_id");
                    table.ForeignKey(
                        name: "FK_TestAttempt_User_student_id",
                        column: x => x.student_id,
                        principalTable: "User",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "TestQuestion",
                columns: table => new
                {
                    test_question_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    test_id = table.Column<int>(type: "int", nullable: false),
                    question_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestQuestion", x => x.test_question_id);
                    table.ForeignKey(
                        name: "FK_TestQuestion_Question_question_id",
                        column: x => x.question_id,
                        principalTable: "Question",
                        principalColumn: "question_id");
                    table.ForeignKey(
                        name: "FK_TestQuestion_Test_test_id",
                        column: x => x.test_id,
                        principalTable: "Test",
                        principalColumn: "test_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestQuestionResult",
                columns: table => new
                {
                    result_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    attempt_id = table.Column<int>(type: "int", nullable: false),
                    question_id = table.Column<int>(type: "int", nullable: false),
                    selected_answer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_correct = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestQuestionResult", x => x.result_id);
                    table.ForeignKey(
                        name: "FK_TestQuestionResult_Question_question_id",
                        column: x => x.question_id,
                        principalTable: "Question",
                        principalColumn: "question_id");
                    table.ForeignKey(
                        name: "FK_TestQuestionResult_TestAttempt_attempt_id",
                        column: x => x.attempt_id,
                        principalTable: "TestAttempt",
                        principalColumn: "attempt_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chapter_subject_id",
                table: "Chapter",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_Lesson_chapter_id",
                table: "Lesson",
                column: "chapter_id");

            migrationBuilder.CreateIndex(
                name: "IX_MentorAssignment_mentor_id",
                table: "MentorAssignment",
                column: "mentor_id");

            migrationBuilder.CreateIndex(
                name: "IX_MentorAssignment_student_id",
                table: "MentorAssignment",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_user_id",
                table: "Notification",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Question_lesson_id",
                table: "Question",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionReport_question_id",
                table: "QuestionReport",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionReport_reported_by",
                table: "QuestionReport",
                column: "reported_by");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPackage_grade_id",
                table: "StudentPackage",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPackage_package_id",
                table: "StudentPackage",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPackage_student_id",
                table: "StudentPackage",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPackage_subject_id",
                table: "StudentPackage",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_Subject_grade_id",
                table: "Subject",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_Test_lesson_id",
                table: "Test",
                column: "lesson_id");

            migrationBuilder.CreateIndex(
                name: "IX_Test_student_id",
                table: "Test",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_Test_subject_id",
                table: "Test",
                column: "subject_id");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempt_student_id",
                table: "TestAttempt",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_TestAttempt_test_id",
                table: "TestAttempt",
                column: "test_id");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestion_question_id",
                table: "TestQuestion",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestion_test_id",
                table: "TestQuestion",
                column: "test_id");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestionResult_attempt_id",
                table: "TestQuestionResult",
                column: "attempt_id");

            migrationBuilder.CreateIndex(
                name: "IX_TestQuestionResult_question_id",
                table: "TestQuestionResult",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_package_id",
                table: "Transaction",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_user_id",
                table: "Transaction",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_grade_id",
                table: "User",
                column: "grade_id");

            migrationBuilder.CreateIndex(
                name: "IX_User_parent_id",
                table: "User",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_Wallet_parent_id",
                table: "Wallet",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_package_id",
                table: "WalletTransaction",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransaction_wallet_id",
                table: "WalletTransaction",
                column: "wallet_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MentorAssignment");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "QuestionReport");

            migrationBuilder.DropTable(
                name: "StudentPackage");

            migrationBuilder.DropTable(
                name: "TestQuestion");

            migrationBuilder.DropTable(
                name: "TestQuestionResult");

            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "WalletTransaction");

            migrationBuilder.DropTable(
                name: "Question");

            migrationBuilder.DropTable(
                name: "TestAttempt");

            migrationBuilder.DropTable(
                name: "Package");

            migrationBuilder.DropTable(
                name: "Wallet");

            migrationBuilder.DropTable(
                name: "Test");

            migrationBuilder.DropTable(
                name: "Lesson");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Chapter");

            migrationBuilder.DropTable(
                name: "Subject");

            migrationBuilder.DropTable(
                name: "Grade");
        }
    }
}
