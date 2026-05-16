using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MentoringApp.Model.User
{
    /// <summary>
    /// Abstract base for all user types.
    /// Concrete subtypes: <see cref="StudentModel"/>, <see cref="SupervisorModel"/>, <see cref="AdminModel"/>.
    /// JSON polymorphism is handled via <see cref="JsonDerivedTypeAttribute"/>.
    /// </summary>
    [JsonDerivedType(typeof(StudentModel), typeDiscriminator: "student")]
    [JsonDerivedType(typeof(SupervisorModel), typeDiscriminator: "supervisor")]
    [JsonDerivedType(typeof(AdminModel), typeDiscriminator: "admin")]
    public abstract partial class UserModel : ObservableObject
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;

        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private string _profilePicturePath = string.Empty;

        [ObservableProperty]
        private string _language = "en";

        [ObservableProperty]
        private string? _phoneNumber;

        [ObservableProperty]
        private Gender _gender = Gender.PreferNoAnswer;

        public bool IsAdmin => this is AdminModel;
        public bool IsStudent => this is StudentModel;
        public bool IsSupervisor => this is SupervisorModel;
        public bool IsMentor => IsStudent && (this as StudentModel)?.IsMentor == true;
        public bool IsMentee => IsStudent && (this as StudentModel)?.IsMentee == true;

        [JsonIgnore]
        public string Role => this switch
        {
            AdminModel => "Admin",
            SupervisorModel => "Supervisor",
            StudentModel => "Student",
            _ => "User"
        };

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
