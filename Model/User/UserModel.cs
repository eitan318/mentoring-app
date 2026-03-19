using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MentoringApp.Model.User
{

    public abstract partial class UserModel : ObservableObject
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private string _profilePicturePath = string.Empty;

        public VerificationCode? CurrentVerificationCode { get; set; }

        protected UserModel() { }

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
