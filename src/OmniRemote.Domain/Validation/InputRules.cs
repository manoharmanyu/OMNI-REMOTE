using System.Globalization;
using System.Text;

namespace OmniRemote.Domain.Validation;

public readonly record struct NameValidation(bool IsValid, string Normalized, string? Error);

/// <summary>
/// Shared input rules used by both the app (usability) and the backend (security boundary).
/// Names are allow-listed by Unicode category so they are safe to display, log and store.
/// </summary>
public static class InputRules
{
    public const int MaxDeviceNameLength = 40;
    public const int MaxRoomNameLength = 30;

    private const string AllowedPunctuation = "-_'.&()#/,+!:";

    public static NameValidation ValidateDisplayName(string? input, int maxLength)
    {
        if (input is null) return new(false, "", "Name is required.");

        if (input.Contains('\r', StringComparison.Ordinal) || input.Contains('\n', StringComparison.Ordinal) || ContainsUnsafeControlChars(input))
            return new(false, "", "Names cannot contain newlines or control characters.");

        var normalized = CollapseWhitespace(input.Normalize(NormalizationForm.FormC).Trim());
        if (normalized.Length == 0) return new(false, "", "Name is required.");
        if (normalized.Length > maxLength) return new(false, normalized, $"Use {maxLength} characters or fewer.");

        foreach (var rune in normalized.EnumerateRunes())
        {
            if (!IsAllowedNameRune(rune))
                return new(false, normalized, "Use letters, numbers, spaces and - _ ' . & ( ) # / , + ! : only.");
        }
        return new(true, normalized, null);
    }

    public static bool IsSafeIdentifier(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length > maxLength) return false;
        foreach (var c in value)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.')) return false;
        }
        return true;
    }

    /// <summary>True for control, format (bidi override, zero-width) and other invisible characters.</summary>
    public static bool ContainsUnsafeControlChars(string value)
    {
        foreach (var rune in value.EnumerateRunes())
        {
            var cat = Rune.GetUnicodeCategory(rune);
            if (cat is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.LineSeparator
                or UnicodeCategory.ParagraphSeparator or UnicodeCategory.PrivateUse or UnicodeCategory.Surrogate
                or UnicodeCategory.OtherNotAssigned)
                return true;
        }
        return false;
    }

    /// <summary>Makes untrusted text safe for single-line structured logs (no CR/LF injection, bounded length).</summary>
    public static string ForLog(string? value, int maxLength = 80)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var sb = new StringBuilder(Math.Min(value.Length, maxLength));
        foreach (var c in value)
        {
            if (sb.Length >= maxLength) { sb.Append('…'); break; }
            sb.Append(char.IsControl(c) || char.GetUnicodeCategory(c) == UnicodeCategory.Format ? '?' : c);
        }
        return sb.ToString();
    }

    private static bool IsAllowedNameRune(Rune rune)
    {
        if (rune.Value == ' ') return true;
        var cat = Rune.GetUnicodeCategory(rune);
        return cat switch
        {
            UnicodeCategory.UppercaseLetter or UnicodeCategory.LowercaseLetter or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.ModifierLetter or UnicodeCategory.OtherLetter
                or UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.DecimalDigitNumber => true,
            _ => rune.IsAscii && AllowedPunctuation.Contains((char)rune.Value, StringComparison.Ordinal),
        };
    }

    private static string CollapseWhitespace(string s)
    {
        var sb = new StringBuilder(s.Length);
        var prevSpace = false;
        foreach (var c in s)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!prevSpace) sb.Append(' ');
                prevSpace = true;
            }
            else
            {
                sb.Append(c);
                prevSpace = false;
            }
        }
        return sb.ToString();
    }
}
