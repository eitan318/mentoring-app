using MentoringApp.Data.Interfaces;
using MentoringApp.Data.SQLEF.DataObject;
using MentoringApp.Model;
using Microsoft.EntityFrameworkCore;

namespace MentoringApp.Data.Access.SQLEF
{
    internal class EFVerificationCodeRepo : IVerificationCodeRepo
    {
        private readonly MentoringDbContext _context;

        public EFVerificationCodeRepo(MentoringDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<bool> SaveAsync(int userId, VerificationCode verificationCode)
        {
            try
            {
                _context.VerificationCodes.Add(new VerificationCodeData
                {
                    UserId = userId,
                    Code = verificationCode.Code,
                    CreationDate = verificationCode.CreationDate
                });

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int?> GetUserIdByCodeAsync(string code)
        {
            var codeData = await _context.VerificationCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == code);

            return codeData?.UserId;
        }

        public async Task<bool> DeleteAsync(int userId)
        {
            try
            {
                var code = await _context.VerificationCodes
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (code != null)
                {
                    _context.VerificationCodes.Remove(code);
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
