using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Core.Catalog;

public sealed record Brand(string Id, string Name, string? Secondary, IReadOnlyList<string> Aliases, IReadOnlySet<DeviceType> Types)
{
    public bool HasSecondary => !string.IsNullOrEmpty(Secondary);

    /// <summary>Index section: "A".."Z" or "#" for names starting with a digit/symbol.</summary>
    public string IndexKey => BrandCatalog.IndexKeyFor(Name);
}

public sealed record BrandSection(string Key, IReadOnlyList<Brand> Brands);

/// <summary>Local, maintainable brand catalogue loaded from an embedded JSON resource.</summary>
public sealed class BrandCatalog
{
    public const string SymbolKey = "#";
    public static readonly IReadOnlyList<string> IndexKeys =
        [.. Enumerable.Range('A', 26).Select(c => ((char)c).ToString()), SymbolKey];

    private readonly IReadOnlyList<Brand> _brands;
    private readonly IReadOnlyDictionary<DeviceType, IReadOnlyList<string>> _top;

    public BrandCatalog(IReadOnlyList<Brand> brands, IReadOnlyDictionary<DeviceType, IReadOnlyList<string>> top)
    {
        _brands = brands;
        _top = top;
    }

    public IReadOnlyList<Brand> All => _brands;

    public static BrandCatalog LoadEmbedded()
    {
        using var stream = typeof(BrandCatalog).Assembly.GetManifestResourceStream("OmniRemote.Core.brands.json")
            ?? throw new InvalidOperationException("Brand catalogue resource missing.");
        var file = JsonSerializer.Deserialize(stream, CatalogJsonContext.Default.BrandFile)
            ?? throw new InvalidOperationException("Brand catalogue is empty.");
        return FromFile(file);
    }

    internal static BrandCatalog FromFile(BrandFile file)
    {
        var brands = file.Brands
            .Select(b => new Brand(b.Id, b.Name, b.Secondary, b.Aliases ?? [],
                b.Types.Select(t => Enum.Parse<DeviceType>(t)).ToHashSet()))
            .ToList();
        var ids = brands.Select(b => b.Id).ToHashSet(StringComparer.Ordinal);
        if (ids.Count != brands.Count) throw new InvalidOperationException("Duplicate brand id in catalogue.");

        var top = file.Top.ToDictionary(
            kv => Enum.Parse<DeviceType>(kv.Key),
            kv => (IReadOnlyList<string>)kv.Value.Where(ids.Contains).ToList());
        return new BrandCatalog(brands, top);
    }

    public Brand? Find(string id) => _brands.FirstOrDefault(b => b.Id == id);

    /// <summary>Brands for a category, optionally filtered by a case/diacritic-insensitive query over names and aliases.</summary>
    public IReadOnlyList<Brand> Search(DeviceType type, string? query)
    {
        var q = Fold(query ?? "");
        return _brands
            .Where(b => b.Types.Contains(type))
            .Where(b => q.Length == 0 || Matches(b, q))
            .OrderBy(b => b.Id == "generic" ? 1 : 0)
            .ThenBy(b => Fold(b.Name), StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<Brand> TopBrands(DeviceType type) =>
        _top.TryGetValue(type, out var ids)
            ? ids.Select(Find).Where(b => b is not null && b.Types.Contains(type)).Select(b => b!).ToList()
            : [];

    /// <summary>Groups results into A–Z and "#" sections, in index order.</summary>
    public static IReadOnlyList<BrandSection> Group(IEnumerable<Brand> brands) =>
        brands.GroupBy(b => b.IndexKey)
            .OrderBy(g => g.Key == SymbolKey ? 1 : 0)
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new BrandSection(g.Key, g.ToList()))
            .ToList();

    public static string IndexKeyFor(string name)
    {
        var folded = Fold(name);
        return folded.Length > 0 && folded[0] is >= 'a' and <= 'z'
            ? char.ToUpperInvariant(folded[0]).ToString()
            : SymbolKey;
    }

    private static bool Matches(Brand b, string q) =>
        Fold(b.Name).Contains(q, StringComparison.Ordinal)
        || (b.Secondary is not null && Fold(b.Secondary).Contains(q, StringComparison.Ordinal))
        || b.Aliases.Any(a => Fold(a).Contains(q, StringComparison.Ordinal));

    /// <summary>Lower-case, strip diacritics and punctuation so "ge" matches "G.E." and "tradfri" matches "TRÅDFRI".</summary>
    internal static string Fold(string s)
    {
        var decomposed = s.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
            else if (c == ' ') sb.Append(' ');
        }
        return sb.ToString();
    }
}

internal sealed class BrandFile
{
    public List<BrandEntry> Brands { get; set; } = [];
    public Dictionary<string, List<string>> Top { get; set; } = [];
}

internal sealed class BrandEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Secondary { get; set; }
    public List<string>? Aliases { get; set; }
    public List<string> Types { get; set; } = [];
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(BrandFile))]
internal sealed partial class CatalogJsonContext : JsonSerializerContext;
