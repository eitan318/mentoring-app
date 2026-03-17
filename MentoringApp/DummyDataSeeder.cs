using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Service;

namespace MentoringApp
{
    /// <summary>
    /// Seeds the database with comprehensive dummy data covering all application flows:
    /// Grades, Subjects, IssueCategories, Users (Admin, Supervisor, Mentors, Mentees),
    /// VerificationCodes, Pairs, Reviews, and Issues.
    /// </summary>
    public class DummyDataSeeder
    {
        private readonly UserService _userService;
        private readonly IPairRepo _pairRepo;
        private readonly IIssueRepo _issueRepo;
        private readonly IReviewRepo _reviewRepo;
        private readonly IVerificationCodeRepo _verificationCodeRepo;
        private readonly ISQLiteConnectionService _db;

        public DummyDataSeeder(
            UserService userService,
            IPairRepo pairRepo,
            IIssueRepo issueRepo,
            IReviewRepo reviewRepo,
            IVerificationCodeRepo verificationCodeRepo,
            ISQLiteConnectionService db)
        {
            _userService = userService;
            _pairRepo = pairRepo;
            _issueRepo = issueRepo;
            _reviewRepo = reviewRepo;
            _verificationCodeRepo = verificationCodeRepo;
            _db = db;
        }

        public async Task SeedAsync()
        {
            // ── Step 1: Lookup tables (Grades, Subjects, IssueCategories) ───────────
            // These tables have no insert method in their repos, so we use raw SQL.

            // Grades
            _db.Execute("INSERT INTO Grades (Name, Num) VALUES ('9th',  9)");
            _db.Execute("INSERT INTO Grades (Name, Num) VALUES ('10th', 10)");
            _db.Execute("INSERT INTO Grades (Name, Num) VALUES ('11th', 11)");
            _db.Execute("INSERT INTO Grades (Name, Num) VALUES ('12th', 12)");

            // Subjects
            _db.Execute("INSERT INTO Subjects (Name) VALUES ('Math')");
            _db.Execute("INSERT INTO Subjects (Name) VALUES ('Physics')");
            _db.Execute("INSERT INTO Subjects (Name) VALUES ('Computer Science')");
            _db.Execute("INSERT INTO Subjects (Name) VALUES ('English')");

            // IssueCategories
            _db.Execute("INSERT INTO IssueCategories (Name) VALUES ('Technical Issue')");
            _db.Execute("INSERT INTO IssueCategories (Name) VALUES ('Behavioral Issue')");
            _db.Execute("INSERT INTO IssueCategories (Name) VALUES ('General Help')");

            // Read back auto-generated IDs
            int grade10  = GetId("SELECT Id FROM Grades WHERE Num = 10");
            int grade11  = GetId("SELECT Id FROM Grades WHERE Num = 11");
            int grade12  = GetId("SELECT Id FROM Grades WHERE Num = 12");

            int subjectMath    = GetId("SELECT Id FROM Subjects WHERE Name = 'Math'");
            int subjectPhysics = GetId("SELECT Id FROM Subjects WHERE Name = 'Physics'");
            int subjectCS      = GetId("SELECT Id FROM Subjects WHERE Name = 'Computer Science'");

            int catTechnical   = GetId("SELECT Id FROM IssueCategories WHERE Name = 'Technical Issue'");
            int catBehavioral  = GetId("SELECT Id FROM IssueCategories WHERE Name = 'Behavioral Issue'");
            int catGeneral     = GetId("SELECT Id FROM IssueCategories WHERE Name = 'General Help'");

            // ── Step 2: Users ────────────────────────────────────────────────────────

            // Admin
            var admin = new Admin
            {
                Email = "admin@mentoringapp.com",
                NationalId = "100000001",
                UserName = "Admin User"
            };
            await _userService.CreateUserAsync(admin);
            // admin.Id is now set

            // Supervisor
            var supervisor = new Supervisor
            {
                Email = "jane.supervisor@mentoringapp.com",
                NationalId = "200000001",
                UserName = "Jane Supervisor"
            };
            await _userService.CreateUserAsync(supervisor);

            // Mentor 1: Alice — teaches Math, 11th grade
            var mentor1 = new Student
            {
                Email = "alice.mentor@mentoringapp.com",
                NationalId = "300000001",
                UserName = "Alice Mentor",
                Grade = new Grade { Id = grade11, Name = "11th", Num = 11 },
                MentorProfile = new MentorProfile { SubjectToTeach = subjectMath }
            };
            await _userService.CreateUserAsync(mentor1);

            // Mentor 2: Bob — teaches Physics, 12th grade
            var mentor2 = new Student
            {
                Email = "bob.mentor@mentoringapp.com",
                NationalId = "300000002",
                UserName = "Bob Mentor",
                Grade = new Grade { Id = grade12, Name = "12th", Num = 12 },
                MentorProfile = new MentorProfile { SubjectToTeach = subjectPhysics }
            };
            await _userService.CreateUserAsync(mentor2);

            // Mentee 1: Charlie — learns Math, 10th grade
            var mentee1 = new Student
            {
                Email = "charlie.mentee@mentoringapp.com",
                NationalId = "400000001",
                UserName = "Charlie Mentee",
                Grade = new Grade { Id = grade10, Name = "10th", Num = 10 },
                MenteeProfile = new MenteeProfile { SubjectToLearn = subjectMath }
            };
            await _userService.CreateUserAsync(mentee1);

            // Mentee 2: Dave — learns Physics, 11th grade
            var mentee2 = new Student
            {
                Email = "dave.mentee@mentoringapp.com",
                NationalId = "400000002",
                UserName = "Dave Mentee",
                Grade = new Grade { Id = grade11, Name = "11th", Num = 11 },
                MenteeProfile = new MenteeProfile { SubjectToLearn = subjectPhysics }
            };
            await _userService.CreateUserAsync(mentee2);

            // Dual-role: Eve — teaches CS, learns Math, 11th grade
            var eveDual = new Student
            {
                Email = "eve.dual@mentoringapp.com",
                NationalId = "500000001",
                UserName = "Eve Dual",
                Grade = new Grade { Id = grade11, Name = "11th", Num = 11 },
                MentorProfile = new MentorProfile { SubjectToTeach = subjectCS },
                MenteeProfile = new MenteeProfile { SubjectToLearn = subjectMath },
                // Assign a verification code so the verification flow can be tested
                CurrentVerificationCode = new VerificationCode("VERIFY-EVE-2024")
            };
            await _userService.CreateUserAsync(eveDual);

            // ── Step 3: Pairs ────────────────────────────────────────────────────────

            // Pair 1: Alice (mentor) + Charlie (mentee), supervised by Jane
            await _pairRepo.CreateAsync(supervisor.Id, mentor1.Id, mentee1.Id);

            // Pair 2: Bob (mentor) + Dave (mentee), supervised by Jane
            await _pairRepo.CreateAsync(supervisor.Id, mentor2.Id, mentee2.Id);

            // Read back actual pair IDs from DB
            int pairId1 = GetId("SELECT Id FROM Pairs WHERE MentorId = " + mentor1.Id + " AND MenteeId = " + mentee1.Id);
            int pairId2 = GetId("SELECT Id FROM Pairs WHERE MentorId = " + mentor2.Id + " AND MenteeId = " + mentee2.Id);

            // ── Step 4: Reviews ──────────────────────────────────────────────────────

            // Alice reviews Charlie's progress (Pair 1)
            _reviewRepo.Create("Charlie is doing great progress in Math! Very consistent effort.", DateTime.UtcNow.AddDays(-7), pairId1, mentor1.Id);

            // Dave reviews Bob's teaching style (Pair 2)
            _reviewRepo.Create("Bob explains Physics concepts very clearly. Loving the sessions!", DateTime.UtcNow.AddDays(-3), pairId2, mentee2.Id);

            // ── Step 5: Issues ───────────────────────────────────────────────────────

            // Issue 1: Charlie (mentee) reports a general help issue — unresolved
            _issueRepo.Create(
                "I can't access my learning materials for Math. The link seems broken.",
                catGeneral,
                mentee1.Id);

            // Issue 2: Alice (mentor) reports a technical issue — resolved
            _issueRepo.Create(
                "The app crashed while I was submitting a review for my mentee.",
                catTechnical,
                mentor1.Id);
            // Resolve issue 2
            var createdIssue2 = _issueRepo.GetAll().OrderByDescending(i => i.Id).FirstOrDefault(i => i.Description.Contains("crashed"));
            if (createdIssue2 != null)
                _issueRepo.Resolve(createdIssue2.Id);

            // Issue 3: Bob (mentor) reports a behavioral issue about a student — unresolved
            _issueRepo.Create(
                "My mentee missed two consecutive sessions without notice.",
                catBehavioral,
                mentor2.Id);

            await Task.CompletedTask;
        }

        /// <summary>
        /// Fetches the single integer ID from a query that returns one row with a column named "Id".
        /// </summary>
        private int GetId(string sql)
        {
            var row = _db.QuerySingle<IdRow>(sql);
            return row?.Id ?? throw new Exception($"Expected a row from: {sql}");
        }

        private class IdRow
        {
            public int Id { get; set; }
        }
    }
}
