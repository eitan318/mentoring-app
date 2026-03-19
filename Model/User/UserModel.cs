using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model.User
{
    public abstract class UserModel
    {
        public int Id { get; set; }
        public required string Email { get; set; }
        public required string UserName { get; set; }
        public required string NationalId { get; set; }
        public string? ProfilePicturePath { get; set; }
        public VerificationCode? CurrentVerificationCode { get; set; }

        // Empty constructor for serialization/tools
        protected UserModel() { }

        [SetsRequiredMembers]
        protected UserModel(int id, string email, string userName, string nationalId)
        {
            Id = id;
            Email = email;
            UserName = userName;
            NationalId = nationalId;
        }

        public bool IsValidProfilePicture(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png";
        }
    }
}
