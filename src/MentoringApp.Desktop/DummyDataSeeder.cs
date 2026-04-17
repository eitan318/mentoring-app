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
        private static readonly Gender[] _genders = { Gender.Male, Gender.Female, Gender.Other, Gender.PreferNoAnswer };
        private static readonly GenderPreference[] _genderPrefs = { GenderPreference.Male, GenderPreference.Female, GenderPreference.NoPreference };
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
            // Grades 1–12 are already inserted by SqlDbRepo.Recreate().
            _db.Execute("INSERT INTO Subjects (Name) VALUES ('Math'), ('Physics'), ('Computer Science'), ('English'), ('Chemistry'), ('Biology')");
            _db.Execute("INSERT INTO IssueCategories (Name) VALUES ('Technical Issue'), ('Behavioral Issue'), ('General Help')");

            var subjectIds = _db.Query<IdRow>("SELECT Id FROM Subjects").Select(r => r.Id).ToList();
            var categoryIds = _db.Query<IdRow>("SELECT Id FROM IssueCategories").Select(r => r.Id).ToList();

            string[] profilePics = SetupProfilePictures();

            // ── Step 2: School configuration ─────────────────────────────────
            // Fixed layout used throughout seeding so students, supervisors, and
            // class slots are all consistent with each other.
            //
            //  Grade 9  → Class 1, Class 2   (managed by Supervisor 1)
            //  Grade 10 → Class 1, Class 2   (managed by Supervisor 2)
            //  Grade 11 → Class 1, Class 2   (managed by Supervisor 3)
            //
            Console.WriteLine("Seeding School Configuration...");

            int g9  = GetId("SELECT Id FROM Grades WHERE Num = 9");
            int g10 = GetId("SELECT Id FROM Grades WHERE Num = 10");
            int g11 = GetId("SELECT Id FROM Grades WHERE Num = 11");

            _db.Execute("INSERT INTO SchoolClasses (GradeId, ClassNum) VALUES " +
                        "(@g9,1),(@g9,2),(@g10,1),(@g10,2),(@g11,1),(@g11,2)",
                        new { g9, g10, g11 });

            int sc9_1  = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g9}  AND ClassNum=1");
            int sc9_2  = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g9}  AND ClassNum=2");
            int sc10_1 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g10} AND ClassNum=1");
            int sc10_2 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g10} AND ClassNum=2");
            int sc11_1 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g11} AND ClassNum=1");
            int sc11_2 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g11} AND ClassNum=2");

            // Mark school config as done so the admin lands on the dashboard directly.
            await _settingsService.SetIsSchoolConfiguredAsync(true);

            // ── Step 3: Settings ──────────────────────────────────────────────
            Console.WriteLine("Seeding Settings...");
            await _settingsService.SetMeetingHoursBarrierAsync(10);
            await _settingsService.SetGlobalLanguageAsync("en");
            await _settingsService.ClearPhase1DeadlineAsync();
            await _settingsService.ClearPhase2DeadlineAsync();
            await _settingsService.SetIsUsersImportedAsync(true);
            await _settingsService.SetIsPhase1CompleteAsync(false);
            await _settingsService.SetIsProcessCompleteAsync(false);

            // ── Step 4: Users — Admin ─────────────────────────────────────────
            Console.WriteLine("Generating Users...");

            var admin = new AdminModel { Email = "eitanamir09@gmail.com", NationalId = "100", UserName = "Admin User" };
            await _userService.CreateUserAsync(admin);

            // ── Step 5: Users — Supervisors ───────────────────────────────────
            // Each supervisor manages both classes of one grade.
            //
            //  Supervisor 1 → Grade 9,  Class 1 & Class 2
            //  Supervisor 2 → Grade 10, Class 1 & Class 2
            //  Supervisor 3 → Grade 11, Class 1 & Class 2

            var sup1 = new SupervisorModel
            {
                Email = "supervisor1@mentoringapp.com",
                NationalId = "2001",
                UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                PhoneNumber = $"05{_rand.Next(10000000, 99999999)}",
                Gender = Pick(_genders)
            };
            await _userService.CreateUserAsync(sup1);
            AssignSupervisorClasses(sup1.Id, sc9_1, sc9_2);

            var sup2 = new SupervisorModel
            {
                Email = "supervisor2@mentoringapp.com",
                NationalId = "2002",
                UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                PhoneNumber = $"05{_rand.Next(10000000, 99999999)}",
                Gender = Pick(_genders)
            };
            await _userService.CreateUserAsync(sup2);
            AssignSupervisorClasses(sup2.Id, sc10_1, sc10_2);

            var sup3 = new SupervisorModel
            {
                Email = "supervisor3@mentoringapp.com",
                NationalId = "2003",
                UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                PhoneNumber = $"05{_rand.Next(10000000, 99999999)}",
                Gender = Pick(_genders)
            };
            await _userService.CreateUserAsync(sup3);
            AssignSupervisorClasses(sup3.Id, sc11_1, sc11_2);

            var supervisors = new List<SupervisorModel> { sup1, sup2, sup3 };

            // ── Step 6: Users — Students ──────────────────────────────────────
            // 2 mentors + 2 mentees per class slot, across all 6 defined slots.
            //
            //  Slot          | Supervisor | Mentors | Mentees
            //  Grade 9  / 1  | sup1       |   2     |   2
            //  Grade 9  / 2  | sup1       |   2     |   2
            //  Grade 10 / 1  | sup2       |   2     |   2
            //  Grade 10 / 2  | sup2       |   2     |   2
            //  Grade 11 / 1  | sup3       |   2     |   2
            //  Grade 11 / 2  | sup3       |   2     |   2

            var classSlots = new[]
            {
                (gradeId: g9,  classNum: 1),
                (gradeId: g9,  classNum: 2),
                (gradeId: g10, classNum: 1),
                (gradeId: g10, classNum: 2),
                (gradeId: g11, classNum: 1),
                (gradeId: g11, classNum: 2),
            };

            List<StudentModel> mentors = new();
            List<StudentModel> mentees = new();
            int studentIndex = 1;

            foreach (var slot in classSlots)
            {
                for (int i = 0; i < 2; i++, studentIndex++)
                {
                    var mentor = new StudentModel
                    {
                        Email = $"mentor{studentIndex}@mentoringapp.com",
                        NationalId = $"3{studentIndex:D4}",
                        UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                        Grade = new Grade { Id = slot.gradeId, Name = "", Num = 0 },
                        ClassNum = slot.classNum,
                        Gender = Pick(_genders),
                        PhoneNumber = $"05{_rand.Next(10000000, 99999999)}",
                        PreferredMenteeGender = Pick(_genderPrefs),
                        MentorProfile = new MentorProfile { SubjectToTeach = Pick(subjectIds), MaxMentees = _rand.Next(1, 4) },
                        ProfilePicturePath = EnableProfilePic() ? Pick(profilePics) : null
                    };
                    await _userService.CreateUserAsync(mentor);
                    mentors.Add(mentor);
                }

                for (int i = 0; i < 2; i++, studentIndex++)
                {
                    var mentee = new StudentModel
                    {
                        Email = $"mentee{studentIndex}@mentoringapp.com",
                        NationalId = $"4{studentIndex:D4}",
                        UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                        Grade = new Grade { Id = slot.gradeId, Name = "", Num = 0 },
                        ClassNum = slot.classNum,
                        Gender = Pick(_genders),
                        PhoneNumber = $"05{_rand.Next(10000000, 99999999)}",
                        PreferredMentorGender = Pick(_genderPrefs),
                        MenteeProfile = new MenteeProfile { SubjectToLearn = Pick(subjectIds) },
                        ProfilePicturePath = EnableProfilePic() ? Pick(profilePics) : null
                    };
                    await _userService.CreateUserAsync(mentee);
                    mentees.Add(mentee);
                }
            }

            // ── Step 6b: Unfilled students (registered but info not completed) ──
            // 2 per class slot: one with no role, one with a role but missing subject.
            // These show up in the supervisor's inactive-students list and drive the
            // fill-progress bars visible in Phase 1 on the admin dashboard.
            int unfilledIndex = 1;
            foreach (var slot in classSlots)
            {
                // No role chosen yet
                var noRole = new StudentModel
                {
                    Email = $"unfilled.norole{unfilledIndex}@mentoringapp.com",
                    NationalId = $"9{unfilledIndex:D4}",
                    UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                    Grade = new Grade { Id = slot.gradeId, Name = "", Num = 0 },
                    ClassNum = slot.classNum,
                    Gender = Pick(_genders),
                    PhoneNumber = $"05{_rand.Next(10000000, 99999999)}"
                };
                await _userService.CreateUserAsync(noRole);
                unfilledIndex++;

                // Role chosen but no subject
                var noSubject = new StudentModel
                {
                    Email = $"unfilled.nosubject{unfilledIndex}@mentoringapp.com",
                    NationalId = $"9{unfilledIndex:D4}",
                    UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                    Grade = new Grade { Id = slot.gradeId, Name = "", Num = 0 },
                    ClassNum = slot.classNum,
                    Gender = Pick(_genders),
                    PhoneNumber = $"05{_rand.Next(10000000, 99999999)}",
                    MentorProfile = new MentorProfile { SubjectToTeach = 0, MaxMentees = 1 }
                };
                await _userService.CreateUserAsync(noSubject);
                unfilledIndex++;
            }

            // Dual-role student in Grade 9, Class 1 (covered by sup1)
            var dualStudent = new StudentModel
            {
                Email = "dual.test@mentoringapp.com",
                NationalId = "50001",
                UserName = "Dual Role Test",
                Grade = new Grade { Id = g9, Name = "", Num = 0 },
                ClassNum = 1,
                Gender = Pick(_genders),
                PhoneNumber = $"05{_rand.Next(10000000, 99999999)}",
                PreferredMentorGender = Pick(_genderPrefs),
                PreferredMenteeGender = Pick(_genderPrefs),
                MentorProfile = new MentorProfile { SubjectToTeach = Pick(subjectIds), MaxMentees = 2 },
                MenteeProfile = new MenteeProfile { SubjectToLearn = Pick(subjectIds) },
                ProfilePicturePath = Pick(profilePics),
                CurrentVerificationCode = new VerificationCode("VERIFY-DUAL-2024")
            };
            await _userService.CreateUserAsync(dualStudent);
            mentors.Add(dualStudent);
            mentees.Add(dualStudent);

            // ── Step 7: Pairs ─────────────────────────────────────────────────
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

            // ── Step 8: Reviews and Issues ────────────────────────────────────
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

            Console.WriteLine("Seeding complete!");
        }

        /// <summary>Inserts rows into SupervisorClasses for the given supervisor and class IDs.</summary>
        private void AssignSupervisorClasses(int supervisorId, params int[] schoolClassIds)
        {
            foreach (var classId in schoolClassIds)
                _db.Execute(
                    "INSERT OR IGNORE INTO SupervisorClasses (SupervisorId, SchoolClassId) VALUES (@supervisorId, @classId)",
                    new { supervisorId, classId });
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
