using ProfanityFilter.Interfaces;
using UltimateMonopoly.Enums;
using UltimateMonopoly.Helpers;
using UltimateMonopoly.Models.ViewModels;
using UltimateMonopoly.Services.Cache;

namespace UltimateMonopoly.Services;

/// <summary>
/// The app's profanity gate (v1-roadmap B1). Combines the Profanity.Detector library (comprehensive
/// built-in list + its own allow-list for the Scunthorpe family) with our DB-backed local list of extra
/// terms, matched against the normalised input. Scoped — reads the cached blocked-word set. Biased to
/// UNDER-block: admins override.
/// </summary>
public class ProfanityService
{
    private readonly IProfanityFilter _library;
    private readonly BlockedWordsCacheService _blockedWords;

    public ProfanityService(IProfanityFilter library,
        BlockedWordsCacheService blockedWords)
    {
        _library = library;
        _blockedWords = blockedWords;
    }

    /// <summary>
    /// Checks <paramref name="input"/> for profanity. <paramref name="includeLocalList"/> (default true)
    /// also matches the DB-backed local list; pass <c>false</c> where that list isn't appropriate — it holds
    /// reserved terms like "admin" (to stop impersonating usernames), which are legitimate in free-text such
    /// as a bug report ("the admin page 500s"). The library check (built-in profanity + evasion normalisation)
    /// always runs.
    /// </summary>
    public async Task<ProfanityResult> Check(string? input, bool includeLocalList = true)
    {
        if (string.IsNullOrWhiteSpace(input))
            return ProfanityResult.Clean;

        input = input.Trim().ToLowerInvariant();

        // 1. Library on the raw text — natural-word matching with its own allow-list (handles
        //    Scunthorpe et al.). Catches standard spellings and phrases.
        if (_library.ContainsProfanity(input))
            return new ProfanityResult(true, FirstLibraryHit(input) ?? input.Trim(), ProfanitySource.Library);

        var normalised = ProfanityNormaliser.Normalise(input);
        if (normalised.Length == 0)
            return ProfanityResult.Clean;

        // 2. Library on the normalised (de-leeted / de-separated) form — catches evasions of common
        //    words ("sh1t", "f.u.c.k").
        if (_library.ContainsProfanity(normalised.ToLowerInvariant()))
            return new ProfanityResult(true, FirstLibraryHit(normalised) ?? normalised, ProfanitySource.Library);

        if (!includeLocalList)
            return ProfanityResult.Clean;

        // 3. Local list — our extra terms (incl. reserved words like "admin"), substring-matched against
        //    the normalised input.
        var blocked = await _blockedWords.GetBlockedWords();
        foreach (var term in blocked)
        {
            if (term.Length > 0 && normalised.Contains(term, StringComparison.Ordinal))
                return new ProfanityResult(true, term, ProfanitySource.LocalList);
        }

        return ProfanityResult.Clean;
    }

    private string? FirstLibraryHit(string text)
        => _library.DetectAllProfanities(text).FirstOrDefault();
}