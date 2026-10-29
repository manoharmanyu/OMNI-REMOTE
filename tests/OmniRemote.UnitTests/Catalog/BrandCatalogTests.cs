using OmniRemote.Core.Catalog;
using OmniRemote.Domain.Devices;
using Xunit;

namespace OmniRemote.UnitTests.Catalog;

public sealed class BrandCatalogTests
{
    private readonly BrandCatalog _catalog = BrandCatalog.LoadEmbedded();

    [Fact]
    public void LoadEmbedded_PopulatesBrands()
    {
        Assert.NotEmpty(_catalog.All);
    }

    [Fact]
    public void TopBrands_ForAirConditioner_ContainsExpected()
    {
        var top = _catalog.TopBrands(DeviceType.AirConditioner);
        Assert.Contains(top, b => b.Id == "carrier");
        Assert.Contains(top, b => b.Id == "daikin");
    }

    [Fact]
    public void Search_MatchesAliasesAndFoldedDiacritics()
    {
        // "GE" alias should match "G.E. (General Electric)"
        var resultsGe = _catalog.Search(DeviceType.AirConditioner, "GE");
        Assert.Contains(resultsGe, b => b.Id == "ge-appliances");

        // Diacritics folding: "tradfri" should match "TRÅDFRI"
        var resultsIkea = _catalog.Search(DeviceType.LightBulb, "tradfri");
        Assert.Contains(resultsIkea, b => b.Id == "ikea");
    }

    [Fact]
    public void Group_ProducesOrderedIndexSections()
    {
        var brands = _catalog.Search(DeviceType.AirConditioner, "");
        var sections = BrandCatalog.Group(brands);

        Assert.NotEmpty(sections);
        // First sections are letters A..Z, followed by '#' if symbols exist
        for (int i = 0; i < sections.Count - 1; i++)
        {
            if (sections[i].Key != BrandCatalog.SymbolKey && sections[i + 1].Key != BrandCatalog.SymbolKey)
            {
                Assert.True(string.CompareOrdinal(sections[i].Key, sections[i + 1].Key) < 0);
            }
        }
    }
}
