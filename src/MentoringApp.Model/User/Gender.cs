namespace MentoringApp.Model.User
{
    /// <summary>Self-reported gender of a user.</summary>
    public enum Gender
    {
        Male = 0,
        Female = 1,
        Other = 2,
        PreferNoAnswer = 3
    }

    /// <summary>Gender the user prefers their mentor/mentee match to be.</summary>
    public enum GenderPreference
    {
        Male = 0,
        Female = 1,
        NoPreference = 2
    }
}
