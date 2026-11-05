using System.Globalization;
using AwesomeAssertions;
using Oire.Sic.Utils;
using Xunit;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic.Tests;

/// <summary>
/// Under <c>dotnet test</c> the catalog is named after the test host, so no translation ever
/// loads: every assertion here is on English source text or on the culture chosen, never on a
/// translated string.
/// </summary>
[Collection(StaticStateCollection.Name)]
public sealed class LocalizationTests: StaticStateTestBase {
    [Fact]
    public void Gettext_ReturnsTheSourceText() {
        _("Add Folder").Should().Be("Add Folder");
    }

    [Fact]
    public void Gettext_FillsPlaceholders() {
        _("{0}x{1}", 640, 480).Should().Be("640x480");
    }

    [Theory]
    [InlineData(1, "1 image")]
    [InlineData(2, "2 images")]
    [InlineData(0, "0 images")]
    public void PluralGettext_PicksTheEnglishForm(long count, string expected) {
        _n("{0} image", "{0} images", count, count).Should().Be(expected);
    }

    [Fact]
    public void PluralGettext_WithoutArguments_ReturnsTheChosenForm() {
        _n("image", "images", 1).Should().Be("image");
        _n("image", "images", 5).Should().Be("images");
    }

    [Fact]
    public void ContextGettext_ReturnsTheSourceText() {
        _p("menu", "Open").Should().Be("Open");
        _pn("menu", "{0} file", "{0} files", 3, 3).Should().Be("3 files");
    }

    [Fact]
    public void SetLanguage_KeepsALanguageThatHasALocaleFolder() {
        Localization.SetLanguage("de");

        Localization.GetCurrentCulture().Name.Should().Be("de");
        CultureInfo.CurrentUICulture.Name.Should().Be("de");
        CultureInfo.DefaultThreadCurrentCulture!.Name.Should().Be("de");
    }

    [Fact]
    public void SetLanguage_FallsBackToEnglishForALanguageWithoutALocaleFolder() {
        Localization.SetLanguage("pl-PL");

        Localization.GetCurrentCulture().Name.Should().Be("en-US");
        CultureInfo.CurrentUICulture.Name.Should().Be("en-US");
    }

    [Fact]
    public void SetLanguage_UsesTheParentLanguageForARegionalVariant() {
        Localization.SetLanguage("de-AT");

        Localization.GetCurrentCulture().Name.Should().Be("de");
    }

    [Fact]
    public void SetLanguage_FallsBackToEnglishForANeutralLanguageWithoutALocaleFolder() {
        Localization.SetLanguage("pl");

        Localization.GetCurrentCulture().Name.Should().Be("en-US");
    }
}
