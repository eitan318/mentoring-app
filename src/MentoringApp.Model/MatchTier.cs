namespace MentoringApp.Model
{
    /// <summary>
    /// Describes which tier of the matching process created this pair.
    /// </summary>
    public enum MatchTier
    {
        /// <summary>Tier 1: Mentee directly requested the mentor and they accepted.</summary>
        Direct = 1,

        /// <summary>Tier 3: Mentee selected from their recommended gallery.</summary>
        GalleryChoice = 3,

        /// <summary>Tier 4: Algorithmic stable match after gallery deadline.</summary>
        AutoMatch = 4,

        /// <summary>Tier 5: Random fallback due to incomplete profile data.</summary>
        FallbackRandom = 5,

        /// <summary>Legacy/Admin created: pair was made manually by an admin.</summary>
        AdminManual = 0
    }
}
