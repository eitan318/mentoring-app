using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Data.Interfaces;
using MentoringApp.Model;
using MentoringApp.Model.User;
using MentoringApp.Model.User.StudentProfiles;
using MentoringApp.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MentoringApp
{
    public class DummyDataSeeder
    {
        private readonly UserService _userService;
        private readonly IPairRepo _pairRepo;
        private readonly IIssueRepo _issueRepo;
        private readonly IReviewRepo _reviewRepo;
        private readonly IVerificationCodeRepo _verificationCodeRepo;
        private readonly ISQLiteConnectionService _db;
        private readonly SettingsService _settingsService;
        private readonly Random _rand;

        private readonly Dictionary<string, string> _placeholderImages = new()
        {
            { "red.png",    "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==" },
            { "green.png",  "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGCA2TzHgAAAABJRU5ErkJggg==" },
            { "blue.png",   "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==" },
            { "yellow.png", "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchxAQAAAABJRU5ErkJggg==" },
            { "gray.png",   "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkaPkPAAILAYzofg2wAAAAAElFTkSuQmCC" }
        };

        private readonly string[] _firstNames = { "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda", "William", "Elizabeth", "David", "Barbara", "Richard", "Susan", "Joseph", "Jessica", "Thomas", "Sarah", "Charles", "Karen", "Daniel", "Lisa", "Matthew", "Betty", "Anthony", "Margaret" };
        private readonly string[] _lastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin" };
        private readonly string[] _reviewTexts = { "Great progress so far!", "Needs to work more on practical exercises.", "Brilliant understanding of the core concepts.", "Always on time and very attentive.", "Struggling a bit with the latest chapter, but improving.", "Excellent sessions, very communicative.", "Could use some more independent practice.", "A pleasure to mentor!", "Very proactive in asking questions.", "Making steady improvements every week." };
        private readonly string[] _issueTexts = { "I can't access my learning materials.", "The app crashed while submitting a review.", "My mentee missed two consecutive sessions.", "Password reset loop.", "Need more practice exercises assigned.", "Cannot upload my homework.", "Video call feature is dropping out.", "I'd like to change my mentoring subject.", "Can't see the latest grade updates.", "My supervisor hasn't replied to my request." };

        public DummyDataSeeder(
            UserService userService,
            IPairRepo pairRepo,
            IIssueRepo issueRepo,
            IReviewRepo reviewRepo,
            IVerificationCodeRepo verificationCodeRepo,
            ISQLiteConnectionService db,
            SettingsService settingsService)
        {
            _userService = userService;
            _pairRepo = pairRepo;
            _issueRepo = issueRepo;
            _reviewRepo = reviewRepo;
            _verificationCodeRepo = verificationCodeRepo;
            _db = db;
            _settingsService = settingsService;
            _rand = new Random((int)DateTime.Now.Ticks);
        }

        public async Task SeedAsync()
        {
            // ── Step 1: Lookup tables ─────────────────────────────────────────
            _db.Execute("INSERT INTO Grades (Name, Num) VALUES ('9th', 9), ('10th', 10), ('11th', 11), ('12th', 12)");
            _db.Execute("INSERT INTO Subjects (Name) VALUES ('Math'), ('Physics'), ('Computer Science'), ('English'), ('Chemistry'), ('Biology')");
            _db.Execute("INSERT INTO IssueCategories (Name) VALUES ('Technical Issue'), ('Behavioral Issue'), ('General Help')");

            var gradeIds = _db.Query<IdRow>("SELECT Id FROM Grades").Select(r => r.Id).ToList();
            var subjectIds = _db.Query<IdRow>("SELECT Id FROM Subjects").Select(r => r.Id).ToList();
            var categoryIds = _db.Query<IdRow>("SELECT Id FROM IssueCategories").Select(r => r.Id).ToList();

            string[] profilePics = SetupProfilePictures();

            // ── Step 2: Settings ──────────────────────────────────────────────
            Console.WriteLine("Seeding Settings...");
            await _settingsService.SetMeetingHoursBarrierAsync(10);
            await _settingsService.SetGlobalLanguageAsync("en");
            // Deadlines left empty (no deadline scheduled by default)
            await _settingsService.ClearPhase1DeadlineAsync();
            await _settingsService.ClearPhase2DeadlineAsync();
            await _settingsService.SetIsPhase1CompleteAsync(false);
            await _settingsService.SetIsProcessCompleteAsync(false);

            // ── Step 3: Users ─────────────────────────────────────────────────
            Console.WriteLine("Generating Users...");

            var admin = new AdminModel { Email = "eitanamir09@gmail.com", NationalId = "100", UserName = "Admin User" };
            await _userService.CreateUserAsync(admin);

            List<SupervisorModel> supervisors = new();
            for (int i = 1; i <= 3; i++)
            {
                var s = new SupervisorModel
                {
                    Email = $"supervisor{i}@mentoringapp.com",
                    NationalId = $"200{i}",
                    UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                    Grade = new Grade { Id = Pick(gradeIds), Name = "", Num = 0 },
                    ClassNum = _rand.Next(1, 4)
                };
                await _userService.CreateUserAsync(s);
                supervisors.Add(s);
            }

            List<StudentModel> mentors = new();
            for (int i = 1; i <= 10; i++)
            {
                var mentor = new StudentModel
                {
                    Email = $"mentor{i}@mentoringapp.com",
                    NationalId = $"3000{i}",
                    UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                    Grade = new Grade { Id = Pick(gradeIds), Name = "", Num = 0 },
                    ClassNum = _rand.Next(1, 4),
                    MentorProfile = new MentorProfile { SubjectToTeach = Pick(subjectIds), MaxMentees = _rand.Next(1, 4) },
                    ProfilePicturePath = EnableProfilePic() ? Pick(profilePics) : null
                };
                await _userService.CreateUserAsync(mentor);
                mentors.Add(mentor);
            }

            List<StudentModel> mentees = new();
            for (int i = 1; i <= 10; i++)
            {
                var mentee = new StudentModel
                {
                    Email = $"mentee{i}@mentoringapp.com",
                    NationalId = $"4000{i}",
                    UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                    Grade = new Grade { Id = Pick(gradeIds), Name = "", Num = 0 },
                    ClassNum = _rand.Next(1, 4),
                    MenteeProfile = new MenteeProfile { SubjectToLearn = Pick(subjectIds) },
                    ProfilePicturePath = EnableProfilePic() ? Pick(profilePics) : null
                };
                await _userService.CreateUserAsync(mentee);
                mentees.Add(mentee);
            }

            // Dual role student
            var dualStudent = new StudentModel
            {
                Email = "dual.test@mentoringapp.com",
                NationalId = "50001",
                UserName = "Dual Role Test",
                Grade = new Grade { Id = Pick(gradeIds), Name = "", Num = 0 },
                ClassNum = _rand.Next(1, 4),
                MentorProfile = new MentorProfile { SubjectToTeach = Pick(subjectIds), MaxMentees = 2 },
                MenteeProfile = new MenteeProfile { SubjectToLearn = Pick(subjectIds) },
                ProfilePicturePath = Pick(profilePics),
                CurrentVerificationCode = new VerificationCode("VERIFY-DUAL-2024")
            };
            await _userService.CreateUserAsync(dualStudent);
            mentors.Add(dualStudent);
            mentees.Add(dualStudent);

            // ── Step 4: Pairs ─────────────────────────────────────────────────
            Console.WriteLine("Generating Pairs...");
            List<int> pairIds = new();

            for (int i = 0; i < 5; i++)
            {
                var sup = Pick(supervisors);
                var men = Pick(mentors);
                var mte = Pick(mentees);

                if (men.Id == mte.Id) continue;

                await _pairRepo.CreateAsync(sup.Id, men.Id, mte.Id);
                int pid = GetId($"SELECT Id FROM Pairs WHERE MentorId = {men.Id} AND MenteeId = {mte.Id} ORDER BY Id DESC LIMIT 1");
                pairIds.Add(pid);
            }

            // ── Step 5: Reviews and Issues ────────────────────────────────────
            Console.WriteLine("Generating Reviews and Issues per Pair...");
            foreach (var pid in pairIds)
            {
                int mentorId = GetId($"SELECT MentorId AS Id FROM Pairs WHERE Id = {pid}");
                int menteeId = GetId($"SELECT MenteeId AS Id FROM Pairs WHERE Id = {pid}");

                int reviewCount = _rand.Next(2, 6);
                for (int i = 0; i < reviewCount; i++)
                {
                    int writerId = _rand.NextDouble() > 0.5 ? mentorId : menteeId;
                    DateTime date = DateTime.UtcNow.AddDays(-_rand.Next(1, 180));
                    double hours = Math.Round(0.5 + _rand.NextDouble() * 2, 1);
                    await _reviewRepo.CreateAsync(Pick(_reviewTexts), date, pid, writerId, hours);
                }

                await _issueRepo.CreateAsync(Pick(_issueTexts), Pick(categoryIds), mentorId);
                await _issueRepo.CreateAsync(Pick(_issueTexts), Pick(categoryIds), menteeId);
                await _issueRepo.CreateAsync(Pick(_issueTexts), Pick(categoryIds), mentorId);

                int resolvedIssueId = GetId($"SELECT Id FROM Issues WHERE ReportedByUserId = {mentorId} ORDER BY Id DESC LIMIT 1");
                await _issueRepo.ResolveAsync(resolvedIssueId);
            }

            Console.WriteLine("Mass Data Seeding complete!");
        }

        private string[] SetupProfilePictures()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MentoringApp", "ProfilePictures");
            Directory.CreateDirectory(folder);

            List<string> paths = new();
            foreach (var kvp in _placeholderImages)
            {
                string destPath = Path.Combine(folder, kvp.Key);
                File.WriteAllBytes(destPath, Convert.FromBase64String(kvp.Value));
                paths.Add(destPath);
            }

            return paths.ToArray();
        }

        private int GetId(string sql)
        {
            var row = _db.QuerySingle<IdRow>(sql);
            return row?.Id ?? throw new Exception($"Expected a row from: {sql}");
        }

        private T Pick<T>(IList<T> list) => list[_rand.Next(list.Count)];
        private bool EnableProfilePic() => _rand.NextDouble() > 0.3;

        private class IdRow { public int Id { get; set; } }
    }
}