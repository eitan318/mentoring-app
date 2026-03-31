using MentoringApp.Data.Interfaces;
using Microsoft.Data.Sqlite;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlDbRepo : IDbRepo
    {
        private readonly string _connectionString;

        public SqlDbRepo(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Recreate()
        {
            // Drop all tables and recreate schema using raw SQL
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var dropSql = @"
                PRAGMA foreign_keys = OFF;
                DROP TABLE IF EXISTS Reviews;
                DROP TABLE IF EXISTS Issues;
                DROP TABLE IF EXISTS IssueCategories;
                DROP TABLE IF EXISTS Pairs;
                DROP TABLE IF EXISTS VerificationCodes;
                DROP TABLE IF EXISTS UserMentors;
                DROP TABLE IF EXISTS UserMentees;
                DROP TABLE IF EXISTS UserSupervisors;
                DROP TABLE IF EXISTS UserAdmins;
                DROP TABLE IF EXISTS UserStudents;
                DROP TABLE IF EXISTS Grades;
                DROP TABLE IF EXISTS Subjects;
                DROP TABLE IF EXISTS Users;
                PRAGMA foreign_keys = ON;

                CREATE TABLE Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserName TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    NationalId TEXT NOT NULL,
                    ProfilePicturePath TEXT NULL
                );

                CREATE TABLE Grades (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Num TEXT NOT NULL
                );

                CREATE TABLE Subjects (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                );

                CREATE TABLE UserStudents (
                    UserId INTEGER PRIMARY KEY,
                    GradeId INTEGER NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );

                CREATE TABLE UserMentors (
                    UserId INTEGER PRIMARY KEY,
                    SubjectToTeach INTEGER NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );

                CREATE TABLE UserMentees (
                    UserId INTEGER PRIMARY KEY,
                    SubjectToLearn INTEGER NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );

                CREATE TABLE UserSupervisors (
                    UserId INTEGER PRIMARY KEY,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );

                CREATE TABLE UserAdmins (
                    UserId INTEGER PRIMARY KEY,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );

                CREATE TABLE VerificationCodes (
                    UserId INTEGER PRIMARY KEY,
                    Code TEXT NOT NULL,
                    CreationDate TEXT NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                );

                CREATE TABLE Pairs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MentorId INTEGER NOT NULL,
                    MenteeId INTEGER NOT NULL,
                    SupervisorId INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (MentorId) REFERENCES Users(Id),
                    FOREIGN KEY (MenteeId) REFERENCES Users(Id),
                    FOREIGN KEY (SupervisorId) REFERENCES Users(Id)
                );

                CREATE TABLE IssueCategories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL
                );

                CREATE TABLE Issues (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Description TEXT NOT NULL,
                    CategoryId INTEGER NOT NULL,
                    ReportedByUserId INTEGER NOT NULL,
                    IsResolved INTEGER NOT NULL DEFAULT 0,
                    CreationDate TEXT NOT NULL,
                    FOREIGN KEY (ReportedByUserId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY (CategoryId) REFERENCES IssueCategories(Id)
                );

                CREATE TABLE Reviews (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PairId INTEGER NOT NULL,
                    AuthorUserId INTEGER NOT NULL,
                    Content TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    AmountOfHours REAL NOT NULL DEFAULT 0,
                    FOREIGN KEY (PairId) REFERENCES Pairs(Id) ON DELETE CASCADE,
                    FOREIGN KEY (AuthorUserId) REFERENCES Users(Id)
                );

                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );
                INSERT OR IGNORE INTO Settings (Key, Value) VALUES ('MeetingHoursBarrier', '10');
            ";

            using var cmd = new SqliteCommand(dropSql, conn);
            cmd.ExecuteNonQuery();
        }
    }
}
