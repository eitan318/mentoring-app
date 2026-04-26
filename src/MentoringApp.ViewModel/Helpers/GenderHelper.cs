namespace MentoringApp.ViewModel.Helpers
{
    public class GenderOption
    {
        public int Value { get; set; }
        public string Display { get; set; } = "";
    }

    public class GenderPreferenceOption
    {
        public int Value { get; set; }
        public string Display { get; set; } = "";
    }

    public static class GenderHelper
    {
        public static readonly IReadOnlyList<GenderOption> GenderOptions = new List<GenderOption>
        {
            new GenderOption { Value = (int)Gender.Male,           Display = "Male" },
            new GenderOption { Value = (int)Gender.Female,         Display = "Female" },
            new GenderOption { Value = (int)Gender.Other,          Display = "Other" },
            new GenderOption { Value = (int)Gender.PreferNoAnswer, Display = "Prefer Not to Answer" },
        };

        public static readonly IReadOnlyList<GenderPreferenceOption> GenderPreferenceOptions = new List<GenderPreferenceOption>
        {
            new GenderPreferenceOption { Value = (int)GenderPreference.Male,         Display = "Male" },
            new GenderPreferenceOption { Value = (int)GenderPreference.Female,       Display = "Female" },
            new GenderPreferenceOption { Value = (int)GenderPreference.NoPreference, Display = "No Preference" },
        };

        public static string GenderToDisplay(int gender) => (Gender)gender switch
        {
            Gender.Male           => "Male",
            Gender.Female         => "Female",
            Gender.Other          => "Other",
            Gender.PreferNoAnswer => "Prefer Not to Answer",
            _                     => "Unknown"
        };

        public static string GenderPreferenceToDisplay(int pref) => (GenderPreference)pref switch
        {
            GenderPreference.Male         => "Male",
            GenderPreference.Female       => "Female",
            GenderPreference.NoPreference => "No Preference",
            _                             => "Unknown"
        };
    }
}
