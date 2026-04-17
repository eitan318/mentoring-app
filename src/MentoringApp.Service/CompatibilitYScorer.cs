using MentoringApp.Model.User;

namespace MentoringApp.Service
{
    /// <summary>
    /// Pure compatibility scoring logic, isolated for easy extension.
    /// No dependencies — all methods are stateless.
    ///
    /// Score breakdown (0–100):
    ///   Subject match    60 pts — mentee's subject matches mentor's subject
    ///   Mentee's gender pref  20 pts — mentor's gender satisfies mentee's preference (or NoPreference)
    ///   Mentor's gender pref  20 pts — mentee's gender satisfies mentor's preference (or NoPreference)
    /// </summary>
    public class CompatibilityScorer
    {
        public double Calculate(
            int? menteeSubjectId,
            int? mentorSubjectId,
            GenderPreference menteeGenderPref,
            Gender mentorGender,
            GenderPreference mentorGenderPref,
            Gender menteeGender)
        {
            double score = 0;

            // Subject match (60 pts)
            if (menteeSubjectId != null && mentorSubjectId != null && menteeSubjectId == mentorSubjectId)
                score += 60;

            // Mentee's gender preference (20 pts)
            score += GenderPreferenceSatisfied(menteeGenderPref, mentorGender) ? 20 : 0;

            // Mentor's gender preference (20 pts)
            score += GenderPreferenceSatisfied(mentorGenderPref, menteeGender) ? 20 : 0;

            return score;
        }

        private static bool GenderPreferenceSatisfied(GenderPreference preference, Gender gender)
        {
            return preference == GenderPreference.NoPreference
                || (preference == GenderPreference.Male && gender == Gender.Male)
                || (preference == GenderPreference.Female && gender == Gender.Female);
        }
    }
}
