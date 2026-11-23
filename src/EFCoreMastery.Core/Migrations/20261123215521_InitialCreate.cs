using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFCoreMastery.Core.Migrations;

/// <inheritdoc />
public partial class _20261123215521_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "dbo");

        migrationBuilder.CreateTable(
            name: "Countries",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Countries", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Courses",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Courses", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Roles",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Students",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Students", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Companies",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CountryId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Companies", x => x.Id);
                table.ForeignKey(
                    name: "FK_Companies_Countries_CountryId",
                    column: x => x.CountryId,
                    principalSchema: "dbo",
                    principalTable: "Countries",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "CourseStudent",
            schema: "dbo",
            columns: table => new
            {
                CoursesId = table.Column<int>(type: "int", nullable: false),
                StudentsId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CourseStudent", x => new { x.CoursesId, x.StudentsId });
                table.ForeignKey(
                    name: "FK_CourseStudent_Courses_CoursesId",
                    column: x => x.CoursesId,
                    principalSchema: "dbo",
                    principalTable: "Courses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CourseStudent_Students_StudentsId",
                    column: x => x.StudentsId,
                    principalSchema: "dbo",
                    principalTable: "Students",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Products",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                CompanyId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
                table.ForeignKey(
                    name: "FK_Products_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalSchema: "dbo",
                    principalTable: "Companies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Age = table.Column<int>(type: "int", nullable: false),
                CompanyId = table.Column<int>(type: "int", nullable: true),
                RoleId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.ForeignKey(
                    name: "FK_Users_Companies_CompanyId",
                    column: x => x.CompanyId,
                    principalSchema: "dbo",
                    principalTable: "Companies",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_Users_Roles_RoleId",
                    column: x => x.RoleId,
                    principalSchema: "dbo",
                    principalTable: "Roles",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateTable(
            name: "UserProfiles",
            schema: "dbo",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Login = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Bio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                UserId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserProfiles", x => x.Id);
                table.ForeignKey(
                    name: "FK_UserProfiles_Users_UserId",
                    column: x => x.UserId,
                    principalSchema: "dbo",
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Companies_CountryId",
            schema: "dbo",
            table: "Companies",
            column: "CountryId");

        migrationBuilder.CreateIndex(
            name: "IX_CourseStudent_StudentsId",
            schema: "dbo",
            table: "CourseStudent",
            column: "StudentsId");

        migrationBuilder.CreateIndex(
            name: "IX_Products_CompanyId",
            schema: "dbo",
            table: "Products",
            column: "CompanyId");

        migrationBuilder.CreateIndex(
            name: "IX_UserProfiles_UserId",
            schema: "dbo",
            table: "UserProfiles",
            column: "UserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_CompanyId",
            schema: "dbo",
            table: "Users",
            column: "CompanyId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_RoleId",
            schema: "dbo",
            table: "Users",
            column: "RoleId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CourseStudent",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "Products",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "UserProfiles",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "Courses",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "Students",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "Users",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "Companies",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "Roles",
            schema: "dbo");

        migrationBuilder.DropTable(
            name: "Countries",
            schema: "dbo");
    }
}
