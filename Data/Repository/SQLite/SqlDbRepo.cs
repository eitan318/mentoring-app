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
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            var dropSql = @"
                PRAGMA foreign_keys = OFF;
                DROP TABLE IF EXISTS MatchScores;
                DROP TABLE IF EXISTS PairRequests;
                DROP TABLE IF EXISTS Reviews;
                DROP TABLE IF EXISTS Issues;
                DROP TABLE IF EXISTS IssueCategories;
                DROP TABLE IF EXISTS Pairs;
                DROP TABLE IF EXISTS VerificationCodes;
                DROP TABLE IF EXISTS UserMentors;
                DROP TABLE IF EXISTS UserMentees;
                DROP TABLE IF EXISTS SupervisorClasses;
                DROP TABLE IF EXISTS UserSupervisors;
                DROP TABLE IF EXISTS UserAdmins;
                DROP TABLE IF EXISTS UserStudents;
                DROP TABLE IF EXISTS SchoolClasses;
                DROP TABLE IF EXISTS Grades;
                DROP TABLE IF EXISTS Subjects;
                DROP TABLE IF EXISTS Users;
                DROP TABLE IF EXISTS Settings;
                PRAGMA foreign_keys = ON;

                CREATE TABLE Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserName TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    NationalId TEXT NOT NULL,
                    ProfilePicturePath TEXT NULL,
                    Language TEXT NOT NULL DEFAULT 'en'
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
                    ClassNum INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );

                CREATE TABLE UserMentors (
                    UserId INTEGER PRIMARY KEY,
                    SubjectToTeach INTEGER NOT NULL,
                    MaxMentees INTEGER NOT NULL DEFAULT 1,
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

                CREATE TABLE SchoolClasses (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    GradeId INTEGER NOT NULL,
                    ClassNum INTEGER NOT NULL,
                    UNIQUE (GradeId, ClassNum),
                    FOREIGN KEY (GradeId) REFERENCES Grades(Id)
                );

                CREATE TABLE SupervisorClasses (
                    SupervisorId INTEGER NOT NULL,
                    SchoolClassId INTEGER NOT NULL,
                    PRIMARY KEY (SupervisorId, SchoolClassId),
                    FOREIGN KEY (SupervisorId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY (SchoolClassId) REFERENCES SchoolClasses(Id) ON DELETE CASCADE
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
                    MatchTier INTEGER NOT NULL DEFAULT 0,
                    IsProfileIncomplete INTEGER NOT NULL DEFAULT 0,
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

                CREATE TABLE PairRequests (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MenteeId INTEGER NOT NULL,
                    MentorId INTEGER NOT NULL,
                    Status TEXT NOT NULL DEFAULT 'Pending',
                    Tier INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL,
                    FOREIGN KEY (MenteeId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY (MentorId) REFERENCES Users(Id) ON DELETE CASCADE
                );

                CREATE TABLE MatchScores (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    MenteeId INTEGER NOT NULL,
                    MentorId INTEGER NOT NULL,
                    ScorePercent REAL NOT NULL DEFAULT 0,
                    FOREIGN KEY (MenteeId) REFERENCES Users(Id) ON DELETE CASCADE,
                    FOREIGN KEY (MentorId) REFERENCES Users(Id) ON DELETE CASCADE
                );

                CREATE TABLE Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );
            ";

            using var cmd = new SqliteCommand(dropSql, conn);
            cmd.ExecuteNonQuery();

        }

        
    }
}