using FluentAssertions;
using MentoringApp.Model.User;
using MentoringApp.Service;
using Xunit;

namespace MentoringApp.Tests.Service
{
    public class CompatibilityScorerTests
    {
        private readonly CompatibilityScorer _scorer = new CompatibilityScorer();

        // ── Helper constants ────────────────────────────────────────────────
        private const int SubjectA = 1;
        private const int SubjectB = 2;

        // ════════════════════════════════════════════════════════════════════
        // Full-score scenarios
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void Calculate_Returns100_WhenSubjectMatchAndBothNoPreference()
        {
            double result = _scorer.Calculate(
                menteeSubjectId: SubjectA, mentorSubjectId: SubjectA,
                menteeGenderPref: GenderPreference.NoPreference, mentorGender: Gender.Male,
                mentorGenderPref: GenderPreference.NoPreference, menteeGender: Gender.Female);

            result.Should().Be(100);
        }

        // ════════════════════════════════════════════════════════════════════
        // Subject match / mismatch combinations
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void Calculate_Returns60_WhenSubjectMatchOnly_BothGenderPreferencesUnmet()
        {
            // mentee wants Male mentor → mentor is Female (unmet)
            // mentor wants Female mentee → mentee is Male (unmet)
            double result = _scorer.Calculate(
                menteeSubjectId: SubjectA, mentorSubjectId: SubjectA,
                menteeGenderPref: GenderPreference.Male,   mentorGender: Gender.Female,
                mentorGenderPref: GenderPreference.Female, menteeGender: Gender.Male);

            result.Should().Be(60);
        }

        [Fact]
        public void Calculate_Returns40_WhenNoSubjectMatch_BothGenderPreferencesSatisfied()
        {
            // different subjects → 0 subject score
            // both NoPreference → +20 +20
            double result = _scorer.Calculate(
                menteeSubjectId: SubjectA, mentorSubjectId: SubjectB,
                menteeGenderPref: GenderPreference.NoPreference, mentorGender: Gender.Male,
                mentorGenderPref: GenderPreference.NoPreference, menteeGender: Gender.Female);

            result.Should().Be(40);
        }

        [Fact]
        public void Calculate_Returns0_WhenNoSubjectMatch_NeitherGenderPreferenceSatisfied()
        {
            double result = _scorer.Calculate(
                menteeSubjectId: SubjectA, mentorSubjectId: SubjectB,
                menteeGenderPref: GenderPreference.Male,   mentorGender: Gender.Female,
                mentorGenderPref: GenderPreference.Female, menteeGender: Gender.Male);

            result.Should().Be(0);
        }

        [Fact]
        public void Calculate_Returns80_WhenSubjectMatch_OnlyMenteeGenderPrefSatisfied()
        {
            // mentee wants Male → mentor is Male (satisfied, +20)
            // mentor wants Female → mentee is Male (unmet, +0)
            double result = _scorer.Calculate(
                menteeSubjectId: SubjectA, mentorSubjectId: SubjectA,
                menteeGenderPref: GenderPreference.Male,   mentorGender: Gender.Male,
                mentorGenderPref: GenderPreference.Female, menteeGender: Gender.Male);

            result.Should().Be(80);
        }

        [Fact]
        public void Calculate_Returns80_WhenSubjectMatch_OnlyMentorGenderPrefSatisfied()
        {
            // mentee wants Female → mentor is Male (unmet, +0)
            // mentor wants NoPreference → always satisfied (+20)
            double result = _scorer.Calculate(
                menteeSubjectId: SubjectA, mentorSubjectId: SubjectA,
                menteeGenderPref: GenderPreference.Female,       mentorGender: Gender.Male,
                mentorGenderPref: GenderPreference.NoPreference, menteeGender: Gender.Female);

            result.Should().Be(80);
        }

        [Fact]
        public void Calculate_Returns60_WhenSubjectMatch_BothGenderPrefsUnmet()
        {
            // Same as BothGenderPreferencesUnmet but named distinctly for clarity
            double result = _scorer.Calculate(
                menteeSubjectId: SubjectA, mentorSubjectId: SubjectA,
                menteeGenderPref: GenderPreference.Male,   mentorGender: Gender.Female,
                mentorGenderPref: GenderPreference.Female, menteeGender: Gender.Male);

            result.Should().Be(60);
        }

        // ════════════════════════════════════════════════════════════════════
        // Null subject-id handling
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void Calculate_Returns0_WhenBothSubjectIdsNull()
        {
            double result = _scorer.Calculate(
                menteeSubjectId: null, mentorSubjectId: null,
                menteeGenderPref: GenderPreference.Male,   mentorGender: Gender.Female,
                mentorGenderPref: GenderPreference.Female, menteeGender: Gender.Male);

            result.Should().Be(0);
        }

        [Fact]
        public void Calculate_Returns60_WhenSubjectMatch_MenteeSubjectIdNull_ReturnZeroSubjectScore()
        {
            // menteeSubjectId = null → no subject score
            // Both NoPreference → +20 +20
            double result = _scorer.Calculate(
                menteeSubjectId: null, mentorSubjectId: SubjectA,
                menteeGenderPref: GenderPreference.NoPreference, mentorGender: Gender.Male,
                mentorGenderPref: GenderPreference.NoPreference, menteeGender: Gender.Female);

            result.Should().Be(40); // 0 (subject null) + 20 + 20
        }

        [Fact]
        public void Calculate_Returns60_WhenSubjectMatch_MentorSubjectIdNull_ReturnsZeroSubjectScore()
        {
            // mentorSubjectId = null → no subject score
            // Both NoPreference → +20 +20
            double result = _scorer.Calculate(
                menteeSubjectId: SubjectA, mentorSubjectId: null,
                menteeGenderPref: GenderPreference.NoPreference, mentorGender: Gender.Male,
                mentorGenderPref: GenderPreference.NoPreference, menteeGender: Gender.Female);

            result.Should().Be(40); // 0 (subject null) + 20 + 20
        }

        [Fact]
        public void Calculate_SubjectMismatch_Returns0ForSubjectScore()
        {
            double result = _scorer.Calculate(
                menteeSubjectId: SubjectA, mentorSubjectId: SubjectB,
                menteeGenderPref: GenderPreference.Male,   mentorGender: Gender.Female,
                mentorGenderPref: GenderPreference.Female, menteeGender: Gender.Male);

            result.Should().Be(0);
        }

        // ════════════════════════════════════════════════════════════════════
        // Gender-only scores (no subject match)
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void Calculate_Returns20_WhenNoSubjectMatch_OnlyMenteeNoPreference()
        {
            // mentee NoPreference → +20; mentor wants Female but mentee is Male → +0
            double result = _scorer.Calculate(
                menteeSubjectId: SubjectA, mentorSubjectId: SubjectB,
                menteeGenderPref: GenderPreference.NoPreference, mentorGender: Gender.Male,
                mentorGenderPref: GenderPreference.Female,       menteeGender: Gender.Male);

            result.Should().Be(20);
        }

        [Fact]
        public void Calculate_Returns20_WhenNoSubjectMatch_OnlyMentorNoPreference()
        {
            // mentee wants Female but mentor is Male → +0; mentor NoPreference → +20
            double result = _scorer.Calculate(
                menteeSubjectId: SubjectA, mentorSubjectId: SubjectB,
                menteeGenderPref: GenderPreference.Female,       mentorGender: Gender.Male,
                mentorGenderPref: GenderPreference.NoPreference, menteeGender: Gender.Female);

            result.Should().Be(20);
        }

        // ════════════════════════════════════════════════════════════════════
        // NoPreference always satisfied regardless of partner gender
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void Calculate_NoPreferenceAlwaysSatisfied_ForMale()
        {
            double result = _scorer.Calculate(
                menteeSubjectId: null, mentorSubjectId: null,
                menteeGenderPref: GenderPreference.NoPreference, mentorGender: Gender.Male,
                mentorGenderPref: GenderPreference.NoPreference, menteeGender: Gender.Male);

            result.Should().Be(40);
        }

        [Fact]
        public void Calculate_NoPreferenceAlwaysSatisfied_ForFemale()
        {
            double result = _scorer.Calculate(
                menteeSubjectId: null, mentorSubjectId: null,
                menteeGenderPref: GenderPreference.NoPreference, mentorGender: Gender.Female,
                mentorGenderPref: GenderPreference.NoPreference, menteeGender: Gender.Female);

            result.Should().Be(40);
        }

        [Fact]
        public void Calculate_NoPreferenceAlwaysSatisfied_ForOther()
        {
            double result = _scorer.Calculate(
                menteeSubjectId: null, mentorSubjectId: null,
                menteeGenderPref: GenderPreference.NoPreference, mentorGender: Gender.Other,
                mentorGenderPref: GenderPreference.NoPreference, menteeGender: Gender.Other);

            result.Should().Be(40);
        }

        // ════════════════════════════════════════════════════════════════════
        // Specific gender-preference matching
        // ════════════════════════════════════════════════════════════════════

        [Fact]
        public void Calculate_MalePreference_SatisfiedByMaleOnly()
        {
            double result = _scorer.Calculate(
                menteeSubjectId: null, mentorSubjectId: null,
                menteeGenderPref: GenderPreference.Male, mentorGender: Gender.Male,
                mentorGenderPref: GenderPreference.NoPreference, menteeGender: Gender.Other);

            result.Should().Be(40); // mentee pref satisfied (+20) + mentor NoPreference (+20)
        }

        [Fact]
        public void Calculate_FemalePreference_SatisfiedByFemaleOnly()
        {
            double result = _scorer.Calculate(
                menteeSubjectId: null, mentorSubjectId: null,
                menteeGenderPref: GenderPreference.Female, mentorGender: Gender.Female,
                mentorGenderPref: GenderPreference.NoPreference, menteeGender: Gender.Other);

            result.Should().Be(40); // mentee pref satisfied (+20) + mentor NoPreference (+20)
        }

        [Fact]
        public void Calculate_MalePreference_NotSatisfiedByFemale()
        {
            double result = _scorer.Calculate(
                menteeSubjectId: null, mentorSubjectId: null,
                menteeGenderPref: GenderPreference.Male, mentorGender: Gender.Female,
                mentorGenderPref: GenderPreference.NoPreference, menteeGender: Gender.Other);

            result.Should().Be(20); // mentee pref NOT satisfied (+0) + mentor NoPreference (+20)
        }
    }
}
