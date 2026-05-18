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

namespace MentoringApp.Service
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

        private readonly string[] _firstNames =
        {
            // Male
            "Shimi", "Dror", "Omer", "Yoav", "Yogev", "Maor", "Yaniv", "Eden", "David", "Daniel",
            "Amit", "Jonatan", "Naor", "Anthony", "Rotem", "Lior", "Alon", "Ben", "Tal", "Nir",
            "Ron", "Ilan", "Kobi", "Eran", "Shai", "Guy", "Matan", "Ofir", "Nadav", "Bar",
            "Ori", "Ran", "Idan", "Eyal", "Dani", "Ronen", "Shahar", "Yuval", "Gilad", "Asaf",
            "Itai", "Elad", "Tomer", "Roi", "Nimrod", "Boaz", "Aviad", "Tamir", "Nevo", "Yotam",
            "Adam", "Ariel", "Barak", "Doron", "Gal", "Hadar", "Izik", "Levi", "Michael", "Noam",
            // Female
            "Rona", "Elizabeth", "Susan", "Noa", "Lian", "Liel", "Karen", "Maaian", "Margaret", "Michal",
            "Maya", "Tali", "Galit", "Shira", "Orly", "Gali", "Ayelet", "Ronit", "Liron", "Hila",
            "Dana", "Naomi", "Sigal", "Yael", "Avital", "Meital", "Inbar", "Ela", "Nili", "Reut",
            "Sapir", "Tamar", "Adi", "Orna", "Limor", "Sivan", "Chen", "Mor", "Raz", "Shani",
            "Anat", "Bat-El", "Dafna", "Efrat", "Fanny", "Gefen", "Hadas", "Iris", "Jenny", "Keren"
        };
        private readonly string[] _lastNames =
        {
            "Baruch", "Shukrun", "Speicer", "Syuniakov", "Leybovits", "Harel-Zeleznik", "Ben-Amram", "Mordechai",
            "Hahmon", "Elkalay", "Kurtz", "Macluf", "Keinan", "Vahaba", "Taylor", "Lachmish",
            "Elazar", "Adaniahu", "Lavi", "Vana", "Cohen", "Levi", "Mizrahi", "Peretz",
            "Shapiro", "Goldberg", "Ben-David", "Haim", "Amar", "Azulay",
            "Biton", "Dahan", "Edri", "Fadida", "Gavriel", "Hadad", "Israeli", "Katz",
            "Ben-Moshe", "Naor", "Ohayon", "Porat", "Ronen", "Segal", "Tzur", "Uziel",
            "Weiss", "Yosef", "Zamir", "Avraham", "Carmeli", "Dvir", "Elian", "Friedman",
            "Gross", "Hendel", "Izhaki", "Jacobson", "Klein", "Lieberman", "Manor", "Nevo",
            "Ophir", "Peled", "Raviv", "Schwartz", "Taub", "Unger", "Vilner", "Wolfson"
        };
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
            // Grades 9–12, 4 classes each = 16 slots.
            // 8 supervisors, each managing 2 slots.
            Console.WriteLine("Seeding School Configuration...");

            int g9 = GetId("SELECT Id FROM Grades WHERE Num = 9");
            int g10 = GetId("SELECT Id FROM Grades WHERE Num = 10");
            int g11 = GetId("SELECT Id FROM Grades WHERE Num = 11");
            int g12 = GetId("SELECT Id FROM Grades WHERE Num = 12");

            _db.Execute(
                "INSERT INTO SchoolClasses (GradeId, ClassNum) VALUES " +
                "(@g9,1),(@g9,2),(@g9,3),(@g9,4)," +
                "(@g10,1),(@g10,2),(@g10,3),(@g10,4)," +
                "(@g11,1),(@g11,2),(@g11,3),(@g11,4)," +
                "(@g12,1),(@g12,2),(@g12,3),(@g12,4)",
                new { g9, g10, g11, g12 });

            int sc9_1  = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g9}  AND ClassNum=1");
            int sc9_2  = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g9}  AND ClassNum=2");
            int sc9_3  = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g9}  AND ClassNum=3");
            int sc9_4  = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g9}  AND ClassNum=4");
            int sc10_1 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g10} AND ClassNum=1");
            int sc10_2 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g10} AND ClassNum=2");
            int sc10_3 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g10} AND ClassNum=3");
            int sc10_4 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g10} AND ClassNum=4");
            int sc11_1 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g11} AND ClassNum=1");
            int sc11_2 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g11} AND ClassNum=2");
            int sc11_3 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g11} AND ClassNum=3");
            int sc11_4 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g11} AND ClassNum=4");
            int sc12_1 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g12} AND ClassNum=1");
            int sc12_2 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g12} AND ClassNum=2");
            int sc12_3 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g12} AND ClassNum=3");
            int sc12_4 = GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={g12} AND ClassNum=4");

            // Mark school config as done so the admin lands on the dashboard directly.
            await _settingsService.SetIsSchoolConfiguredAsync(false);

            // ── Step 3: Settings ──────────────────────────────────────────────
            Console.WriteLine("Seeding Settings...");
            await _settingsService.SetMeetingHoursBarrierAsync(10);
            await _settingsService.SetGlobalLanguageAsync("en");
            await _settingsService.ClearPhase1DeadlineAsync();
            await _settingsService.ClearPhase2DeadlineAsync();
            await _settingsService.SetIsUsersImportedAsync(false);
            await _settingsService.SetIsPhase1CompleteAsync(false);
            await _settingsService.SetIsProcessCompleteAsync(false);

            // ── Step 4: Users — Admin ─────────────────────────────────────────
            Console.WriteLine("Generating Users...");

            var admin = new AdminModel { Email = "eitanamir09@gmail.com", NationalId = "100", UserName = "Admin User" };
            await _userService.CreateUserAsync(admin);

            // ── Step 5: Users — Supervisors ───────────────────────────────────
            // 8 supervisors, each managing 2 class slots (one grade, two classes).
            //
            //  sup1 → Grade 9,  Class 1 & 2
            //  sup2 → Grade 9,  Class 3 & 4
            //  sup3 → Grade 10, Class 1 & 2
            //  sup4 → Grade 10, Class 3 & 4
            //  sup5 → Grade 11, Class 1 & 2
            //  sup6 → Grade 11, Class 3 & 4
            //  sup7 → Grade 12, Class 1 & 2
            //  sup8 → Grade 12, Class 3 & 4

            SupervisorModel MakeSupervisor(string email, string nationalId) => new()
            {
                Email = email,
                NationalId = nationalId,
                UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                PhoneNumber = $"05{_rand.Next(10000000, 99999999)}",
                Gender = Pick(_genders)
            };

            var sup1 = MakeSupervisor("supervisor1@mentoringapp.com", "2001");
            await _userService.CreateUserAsync(sup1);
            AssignSupervisorClasses(sup1.Id, sc9_1, sc9_2);

            var sup2 = MakeSupervisor("supervisor2@mentoringapp.com", "2002");
            await _userService.CreateUserAsync(sup2);
            AssignSupervisorClasses(sup2.Id, sc9_3, sc9_4);

            var sup3 = MakeSupervisor("supervisor3@mentoringapp.com", "2003");
            await _userService.CreateUserAsync(sup3);
            AssignSupervisorClasses(sup3.Id, sc10_1, sc10_2);

            var sup4 = MakeSupervisor("supervisor4@mentoringapp.com", "2004");
            await _userService.CreateUserAsync(sup4);
            AssignSupervisorClasses(sup4.Id, sc10_3, sc10_4);

            var sup5 = MakeSupervisor("supervisor5@mentoringapp.com", "2005");
            await _userService.CreateUserAsync(sup5);
            AssignSupervisorClasses(sup5.Id, sc11_1, sc11_2);

            var sup6 = MakeSupervisor("supervisor6@mentoringapp.com", "2006");
            await _userService.CreateUserAsync(sup6);
            AssignSupervisorClasses(sup6.Id, sc11_3, sc11_4);

            var sup7 = MakeSupervisor("supervisor7@mentoringapp.com", "2007");
            await _userService.CreateUserAsync(sup7);
            AssignSupervisorClasses(sup7.Id, sc12_1, sc12_2);

            var sup8 = MakeSupervisor("supervisor8@mentoringapp.com", "2008");
            await _userService.CreateUserAsync(sup8);
            AssignSupervisorClasses(sup8.Id, sc12_3, sc12_4);

            var supervisors = new List<SupervisorModel> { sup1, sup2, sup3, sup4, sup5, sup6, sup7, sup8 };

            // ── Step 6: Users — Students ──────────────────────────────────────
            // 6 mentors + 8 mentees per slot across all 16 class slots.
            var classSlots = new[]
            {
                (gradeId: g9,  classNum: 1),
                (gradeId: g9,  classNum: 2),
                (gradeId: g9,  classNum: 3),
                (gradeId: g9,  classNum: 4),
                (gradeId: g10, classNum: 1),
                (gradeId: g10, classNum: 2),
                (gradeId: g10, classNum: 3),
                (gradeId: g10, classNum: 4),
                (gradeId: g11, classNum: 1),
                (gradeId: g11, classNum: 2),
                (gradeId: g11, classNum: 3),
                (gradeId: g11, classNum: 4),
                (gradeId: g12, classNum: 1),
                (gradeId: g12, classNum: 2),
                (gradeId: g12, classNum: 3),
                (gradeId: g12, classNum: 4),
            };

            List<StudentModel> mentors = new();
            List<StudentModel> mentees = new();
            int studentIndex = 1;

            foreach (var slot in classSlots)
            {
                for (int i = 0; i < 6; i++, studentIndex++)
                {
                    var mentor = new StudentModel
                    {
                        Email = $"mentor{studentIndex}@mentoringapp.com",
                        NationalId = $"3{studentIndex:D4}",
                        UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                        Grade = new GradeModel { Id = slot.gradeId, Name = "", Num = 0 },
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

                for (int i = 0; i < 8; i++, studentIndex++)
                {
                    var mentee = new StudentModel
                    {
                        Email = $"mentee{studentIndex}@mentoringapp.com",
                        NationalId = $"4{studentIndex:D4}",
                        UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                        Grade = new GradeModel { Id = slot.gradeId, Name = "", Num = 0 },
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

            // ── Step 6b: Unfilled students — 3 per slot ───────────────────────
            int unfilledIndex = 1;
            foreach (var slot in classSlots)
            {
                // No role chosen yet
                var noRole = new StudentModel
                {
                    Email = $"unfilled.norole{unfilledIndex}@mentoringapp.com",
                    NationalId = $"9{unfilledIndex:D4}",
                    UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                    Grade = new GradeModel { Id = slot.gradeId, Name = "", Num = 0 },
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
                    Grade = new GradeModel { Id = slot.gradeId, Name = "", Num = 0 },
                    ClassNum = slot.classNum,
                    Gender = Pick(_genders),
                    PhoneNumber = $"05{_rand.Next(10000000, 99999999)}",
                    MentorProfile = new MentorProfile { SubjectToTeach = 0, MaxMentees = 1 }
                };
                await _userService.CreateUserAsync(noSubject);
                unfilledIndex++;

                // Mentee role but no subject
                var noSubjectMentee = new StudentModel
                {
                    Email = $"unfilled.mentee{unfilledIndex}@mentoringapp.com",
                    NationalId = $"9{unfilledIndex:D4}",
                    UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                    Grade = new GradeModel { Id = slot.gradeId, Name = "", Num = 0 },
                    ClassNum = slot.classNum,
                    Gender = Pick(_genders),
                    PhoneNumber = $"05{_rand.Next(10000000, 99999999)}",
                    MenteeProfile = new MenteeProfile { SubjectToLearn = 0 }
                };
                await _userService.CreateUserAsync(noSubjectMentee);
                unfilledIndex++;
            }

            // Dual-role student in Grade 9, Class 1 (covered by sup1)
            var dualStudent = new StudentModel
            {
                Email = "dual.test@mentoringapp.com",
                NationalId = "50001",
                UserName = "Dual Role Test",
                Grade = new GradeModel { Id = g9, Name = "", Num = 0 },
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

            for (int i = 0; i < 30; i++)
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
