using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace UltimateMonopoly.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed partial class HexColourAttribute : ValidationAttribute
{
    [GeneratedRegex(@"^#(?:[0-9A-Fa-f]{3}){1,2}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColourRegex();

    public HexColourAttribute()
        : base("The {0} field must be a valid hex colour in the form #RGB or #RRGGBB.")
    {
    }

    public static bool IsHexColour(string? value) =>
        value is not null && HexColourRegex().IsMatch(value);

    public override bool IsValid(object? value)
    {
        if (value is null) return true;
        return value is string s && HexColourRegex().IsMatch(s);
    }
}