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
        // 0.1 → ~1 supervisor, ~6 users.  1.0 → ~8 supervisors, ~300 users.
        private const float Scale = 0.1f;

        private readonly UserService _userService;
        private readonly IPairRepo _pairRepo;
        private readonly IIssueRepo _issueRepo;
        private readonly IReviewRepo _reviewRepo;
        private readonly IVerificationCodeRepo _verificationCodeRepo;
        private readonly ISQLiteConnectionService _db;
        private readonly SettingsService _settingsService;
        private readonly Random _rand;

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
            // Counts derived from Scale
            int numSupervisors  = Math.Max(1, (int)Math.Round(Scale * 8));
            int numSlots        = numSupervisors * 2; // 2 class slots per supervisor
            int mentorsPerSlot  = Math.Max(1, (int)Math.Round(Scale * 6));
            int menteesPerSlot  = Math.Max(1, (int)Math.Round(Scale * 8));
            int unfilledPerSlot = (int)Math.Round(Scale * 3); // 0–3
            int numPairs        = Math.Max(1, (int)Math.Round(Scale * 30));

            // ── Step 1: Lookup tables ─────────────────────────────────────────
            _db.Execute("INSERT INTO Subjects (Name) VALUES ('Math'), ('Physics'), ('Computer Science'), ('English'), ('Chemistry'), ('Biology')");
            _db.Execute("INSERT INTO IssueCategories (Name) VALUES ('Technical Issue'), ('Behavioral Issue'), ('General Help')");

            var subjectIds  = _db.Query<IdRow>("SELECT Id FROM Subjects").Select(r => r.Id).ToList();
            var categoryIds = _db.Query<IdRow>("SELECT Id FROM IssueCategories").Select(r => r.Id).ToList();

            string[] profilePics = SetupProfilePictures();

            // ── Step 2: School configuration ─────────────────────────────────
            Console.WriteLine("Seeding School Configuration...");

            int g9  = GetId("SELECT Id FROM Grades WHERE Num = 9");
            int g10 = GetId("SELECT Id FROM Grades WHERE Num = 10");
            int g11 = GetId("SELECT Id FROM Grades WHERE Num = 11");
            int g12 = GetId("SELECT Id FROM Grades WHERE Num = 12");

            // 16 possible slots ordered grade-first; take only what Scale needs
            var allSlotDefs = new List<(int gradeId, int classNum)>();
            foreach (var gid in new[] { g9, g10, g11, g12 })
                for (int cn = 1; cn <= 4; cn++)
                    allSlotDefs.Add((gid, cn));

            var activeSlotDefs = allSlotDefs.Take(numSlots).ToList();

            foreach (var (gid, cn) in activeSlotDefs)
                _db.Execute(
                    "INSERT INTO SchoolClasses (GradeId, ClassNum) VALUES (@gid, @cn)",
                    new { gid, cn });

            var slotIds = activeSlotDefs
                .Select(s => GetId($"SELECT Id FROM SchoolClasses WHERE GradeId={s.gradeId} AND ClassNum={s.classNum}"))
                .ToList();

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
            // Each supervisor manages slotIds[s*2] and slotIds[s*2+1].
            SupervisorModel MakeSupervisor(string email, string nationalId) => new()
            {
                Email = email,
                NationalId = nationalId,
                UserName = $"{Pick(_firstNames)} {Pick(_lastNames)}",
                PhoneNumber = $"05{_rand.Next(10000000, 99999999)}",
                Gender = Pick(_genders)
            };

            var supervisors = new List<SupervisorModel>();
            for (int s = 0; s < numSupervisors; s++)
            {
                var sup = MakeSupervisor($"supervisor{s + 1}@mentoringapp.com", $"200{s + 1}");
                await _userService.CreateUserAsync(sup);
                AssignSupervisorClasses(sup.Id, slotIds[s * 2], slotIds[s * 2 + 1]);
                supervisors.Add(sup);
            }

            // ── Step 6: Users — Students ──────────────────────────────────────
            List<StudentModel> mentors = new();
            List<StudentModel> mentees = new();
            int studentIndex = 1;

            foreach (var slot in activeSlotDefs)
            {
                for (int i = 0; i < mentorsPerSlot; i++, studentIndex++)
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
                        ProfilePicturePath = PickProfilePic(profilePics)
                    };
                    await _userService.CreateUserAsync(mentor);
                    mentors.Add(mentor);
                }

                for (int i = 0; i < menteesPerSlot; i++, studentIndex++)
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
                        ProfilePicturePath = PickProfilePic(profilePics)
                    };
                    await _userService.CreateUserAsync(mentee);
                    mentees.Add(mentee);
                }
            }

            // ── Step 6b: Unfilled students ────────────────────────────────────
            if (unfilledPerSlot > 0)
            {
                int unfilledIndex = 1;
                foreach (var slot in activeSlotDefs)
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

                    if (unfilledPerSlot >= 2)
                    {
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
                    }

                    if (unfilledPerSlot >= 3)
                    {
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
                }
            }

            // Dual-role student always in Grade 9, Class 1
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
                ProfilePicturePath = PickProfilePic(profilePics),
                CurrentVerificationCode = new VerificationCode("VERIFY-DUAL-2024")
            };
            await _userService.CreateUserAsync(dualStudent);
            mentors.Add(dualStudent);
            mentees.Add(dualStudent);

            // ── Step 7: Pairs ─────────────────────────────────────────────────
            Console.WriteLine("Generating Pairs...");
            List<int> pairIds = new();
            var usedPairs = new HashSet<(int, int)>();

            for (int i = 0; i < numPairs; i++)
            {
                var sup = Pick(supervisors);
                var men = Pick(mentors);
                var mte = Pick(mentees);

                if (men.Id == mte.Id) continue;
                if (!usedPairs.Add((men.Id, mte.Id))) continue;

                bool created = await _pairRepo.CreateAsync(sup.Id, men.Id, mte.Id);
                if (!created) continue;

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

        private void AssignSupervisorClasses(int supervisorId, params int[] schoolClassIds)
        {
            foreach (var classId in schoolClassIds)
                _db.Execute(
                    "INSERT OR IGNORE INTO SupervisorClasses (SupervisorId, SchoolClassId) VALUES (@supervisorId, @classId)",
                    new { supervisorId, classId });
        }

        private string[] SetupProfilePictures()
        {
            string destFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MentoringApp", "ProfilePictures");
            Directory.CreateDirectory(destFolder);

            string? seedFolder = FindSeedImagesFolder();
            if (seedFolder == null)
                return Array.Empty<string>();

            var sourceFiles = Directory.GetFiles(seedFolder, "*.jpg")
                .Concat(Directory.GetFiles(seedFolder, "*.png"))
                .ToArray();

            List<string> paths = new();
            foreach (var src in sourceFiles)
            {
                string dest = Path.Combine(destFolder, Path.GetFileName(src));
                if (!File.Exists(dest))
                    File.Copy(src, dest);
                paths.Add(dest);
            }

            return paths.ToArray();
        }

        // Walks up from the app's base directory until it finds a seed-images subfolder.
        private static string? FindSeedImagesFolder()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "seed-images");
                if (Directory.Exists(candidate))
                    return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        private int GetId(string sql)
        {
            var row = _db.QuerySingle<IdRow>(sql);
            return row?.Id ?? throw new Exception($"Expected a row from: {sql}");
        }

        private T Pick<T>(IList<T> list) => list[_rand.Next(list.Count)];

        private string? PickProfilePic(string[] pics) =>
            pics.Length > 0 && _rand.NextDouble() > 0.3 ? Pick(pics) : null;

        private class IdRow { public int Id { get; set; } }
    }
}
