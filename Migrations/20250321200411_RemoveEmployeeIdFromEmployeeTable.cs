using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Employee_Attendance_Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmployeeIdFromEmployeeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS Employees (
                    Id INT NOT NULL AUTO_INCREMENT,
                    Name LONGTEXT NOT NULL,
                    Username LONGTEXT NOT NULL,
                    PasswordHash LONGTEXT NOT NULL,
                    IsAdmin TINYINT(1) NOT NULL,
                    PRIMARY KEY (Id)
                ) CHARACTER SET=utf8mb4;
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS MonthlyWorks (
                    Id INT NOT NULL AUTO_INCREMENT,
                    EmployeeId INT NOT NULL,
                    Date DATETIME(6) NOT NULL,
                    WorkedMinutes INT NOT NULL,
                    PRIMARY KEY (Id),
                    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS WorkHours (
                    Id INT NOT NULL AUTO_INCREMENT,
                    EmployeeId INT NOT NULL,
                    CheckIn DATETIME(6) NOT NULL,
                    CheckOut DATETIME(6) NULL,
                    PRIMARY KEY (Id),
                    FOREIGN KEY (EmployeeId) REFERENCES Employees(Id) ON DELETE CASCADE
                ) CHARACTER SET=utf8mb4;
            ");

            migrationBuilder.Sql(@"
                CREATE PROCEDURE CreateIndexIfNotExists()
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'MonthlyWorks' AND INDEX_NAME = 'IX_MonthlyWorks_EmployeeId') THEN
                        CREATE INDEX IX_MonthlyWorks_EmployeeId ON MonthlyWorks (EmployeeId);
                    END IF;
                END;
                CALL CreateIndexIfNotExists();
                DROP PROCEDURE CreateIndexIfNotExists;
            ");

            migrationBuilder.Sql(@"
                CREATE PROCEDURE CreateIndexIfNotExistsWorkHours()
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'WorkHours' AND INDEX_NAME = 'IX_WorkHours_EmployeeId') THEN
                        CREATE INDEX IX_WorkHours_EmployeeId ON WorkHours (EmployeeId);
                    END IF;
                END;
                CALL CreateIndexIfNotExistsWorkHours();
                DROP PROCEDURE CreateIndexIfNotExistsWorkHours;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyWorks");

            migrationBuilder.DropTable(
                name: "WorkHours");

            migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}
