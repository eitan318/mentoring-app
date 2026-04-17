using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Data.DTO;
using MentoringApp.Data.Interfaces;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlSchoolClassRepo : ISchoolClassRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlSchoolClassRepo(ISQLiteConnectionService db) => _db = db;

        public async Task<IEnumerable<SchoolClassDto>> GetAllAsync()
        {
            const string sql = "SELECT Id, GradeId, ClassNum FROM SchoolClasses ORDER BY GradeId, ClassNum";
            return await _db.QueryAsync<SchoolClassDto>(sql);
        }

        public async Task<IEnumerable<SchoolClassDto>> GetBySupervisorAsync(int supervisorId)
        {
            const string sql = @"
                SELECT sc.Id, sc.GradeId, sc.ClassNum
                FROM SchoolClasses sc
                INNER JOIN SupervisorClasses svc ON svc.SchoolClassId = sc.Id
                WHERE svc.SupervisorId = @supervisorId
                ORDER BY sc.GradeId, sc.ClassNum";
            return await _db.QueryAsync<SchoolClassDto>(sql, new { supervisorId });
        }

        public async Task<bool> AddAsync(int gradeId, int classNum)
        {
            const string sql = @"
                INSERT OR IGNORE INTO SchoolClasses (GradeId, ClassNum) VALUES (@gradeId, @classNum)";
            return await _db.ExecuteAsync(sql, new { gradeId, classNum }) > 0;
        }

        public async Task<bool> DeleteAsync(int schoolClassId)
        {
            // SupervisorClasses CASCADE deletes will handle the mapping rows
            const string sql = "DELETE FROM SchoolClasses WHERE Id = @schoolClassId";
            return await _db.ExecuteAsync(sql, new { schoolClassId }) > 0;
        }

        public async Task SetSupervisorClassesAsync(int supervisorId, IEnumerable<int> schoolClassIds)
        {
            // Delete all existing assignments for this supervisor then re-insert
            const string deleteSQL = "DELETE FROM SupervisorClasses WHERE SupervisorId = @supervisorId";
            await _db.ExecuteAsync(deleteSQL, new { supervisorId });

            foreach (var id in schoolClassIds)
            {
                const string insertSQL = @"INSERT OR IGNORE INTO SupervisorClasses (SupervisorId, SchoolClassId)
                                          VALUES (@supervisorId, @schoolClassId)";
                await _db.ExecuteAsync(insertSQL, new { supervisorId, schoolClassId = id });
            }
        }
    }
}
