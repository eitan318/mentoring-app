using MentoringApp.Data.Interfaces;
using MentoringApp.Data.Acess.SQLite.ConnectionsService;
using MentoringApp.Data.DTO;

namespace MentoringApp.Data.Acess.SQLite
{
    internal class SqlVerificationCodeRepo : IVerificationCodeRepo
    {
        private readonly ISQLiteConnectionService _db;

        public SqlVerificationCodeRepo(ISQLiteConnectionService db)
        {
            _db = db;
        }

        public Task<bool> SaveAsync(int userId, string code, DateTime creationDate)
        {
            try
            {
                _db.Execute(
                    @"INSERT INTO VerificationCodes (UserId, Code, CreationDate)
                      VALUES (@UserId, @Code, @CreationDate)
                      ON CONFLICT(UserId) DO UPDATE SET Code = excluded.Code, CreationDate = excluded.CreationDate",
                    new
                    {
                        UserId = userId,
                        Code = code,
                        CreationDate = creationDate.ToString("o")
                    });
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<int?> GetUserIdByCodeAsync(string code)
        {
            var row = _db.QuerySingle<CodeRow>(
                "SELECT UserId FROM VerificationCodes WHERE Code = @Code",
                new { Code = code });

            int? result = row == null ? null : (int?)row.UserId;
            return Task.FromResult(result);
        }

        public Task<bool> DeleteAsync(int userId)
        {
            try
            {
                _db.Execute(
                    "DELETE FROM VerificationCodes WHERE UserId = @UserId",
                    new { UserId = userId });
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private class CodeRow
        {
            public int UserId { get; set; }
        }
    }
}
