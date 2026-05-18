using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;

namespace MentoringApp.Data.Acess.SQLite
{
    /// <summary>
    /// Handles the bulk data mutations needed at the end of each academic year:
    ///   1. Identify students in the highest grade (graduating class).
    ///   2. Advance all remaining students' grade to the next one in sequence.
    /// </summary>
    internal class SqlYearAdvanceRepo : IYearAdvanceRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlYearAdvanceRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<int>> GetGraduatingStudentIdsAsync()
        {
            // Students whose grade has the maximum Num value
            const string sql = @"
                SELECT us.UserId
                FROM UserStudents us
                INNER JOIN Grades g ON g.Id = us.GradeId
                WHERE g.Num = (SELECT MAX(Num) FROM Grades)";

            var rows = await _db.QueryAsync<UserIdRow>(sql);
            return rows.Select(r => r.UserId).ToList();
        }

        /// <inheritdoc/>
        public async Task AdvanceStudentGradesAsync()
        {
            // For each student, set GradeId to the grade whose Num is the next
            // higher value. Students already in the top grade are left unchanged
            // (they will have been deleted before this is called).
            const string sql = @"
                UPDATE UserStudents
                SET GradeId = (
                    SELECT g2.Id
                    FROM Grades g2
                    INNER JOIN Grades g1 ON g1.Id = UserStudents.GradeId
                    WHERE g2.Num = (
                        SELECT MIN(g3.Num)
                        FROM Grades g3
                        WHERE g3.Num > g1.Num
                    )
                )
                WHERE EXISTS (
                    SELECT 1
                    FROM Grades gx
                    WHERE gx.Id = UserStudents.GradeId
                      AND gx.Num < (SELECT MAX(Num) FROM Grades)
                )";

            await _db.ExecuteAsync(sql);
        }

        private class UserIdRow
        {
            public int UserId { get; set; }
        }
    }
}
