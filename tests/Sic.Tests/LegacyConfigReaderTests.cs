using System.Text;
using AwesomeAssertions;
using Oire.Sic.Models;
using Oire.Sic.Utils;
using Oire.Sic.Utils.Enums;
using Xunit;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic.Tests;

/// <summary>
/// The fixtures under <c>Fixtures\LegacyConfig</c> were written by the real SharpConfig
/// (<c>Section.FromObject</c> plus <c>SaveToFile</c>) before the package was removed, except
/// <c>four-keys.cfg</c>, which is hand-written to match the oldest released files.
/// </summary>
public sealed class LegacyConfigReaderTests: IDisposable {
    private static readonly string FixtureFolder = Path.Combine(AppContext.BaseDirectory, "Fixtures", "LegacyConfig");

    private readonly string _tempFolder = Path.Combine(Path.GetTempPath(), "Sic.Tests", Guid.NewGuid().ToString("N"));

    public LegacyConfigReaderTests() {
        Directory.CreateDirectory(_tempFolder);
    }

    public void Dispose() {
        if (Directory.Exists(_tempFolder)) {
            Directory.Delete(_tempFolder, recursive: true);
        }
    }

    public static TheoryData<string, string, bool> PathFixtures() {
        var data = new TheoryData<string, string, bool>();
        string[][] vectors = [
            ["path-hash.cfg", @"C:\Pics #1\out"],
            ["path-semicolon.cfg", @"C:\a;b\out"],
            ["path-equals.cfg", @"C:\a=b\out"],
            ["path-comma.cfg", @"C:\a,b\out"],
            ["path-braces.cfg", @"C:\{x}\out"],
            ["path-space.cfg", @"C:\a b\out"],
            ["path-trailing-backslash.cfg", @"C:\a\"],
            ["path-space-trailing-backslash.cfg", @"C:\a b\"],
            ["path-backslash-hash.cfg", @"C:\back\#slash"],
            ["path-cyrillic.cfg", @"C:\Фото\Блог"],
            ["path-hebrew.cfg", @"C:\תמונות\יצוא"],
        ];

        foreach (var vector in vectors) {
            data.Add(vector[0], vector[1], false);
            data.Add(vector[0], vector[1], true);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(PathFixtures))]
    public void Read_PathFixture_ReturnsEveryValueExactly(string fileName, string expectedPath, bool withBom) {
        var settings = LegacyConfigReader.Read(FixturePath(fileName, withBom));

        settings.Language.Should().Be("de");
        settings.OutputFolder.Should().Be(expectedPath);
        settings.SaveToSourceFolder.Should().BeTrue();
        settings.LastInputFolder.Should().Be(expectedPath);
        settings.ConfirmExitWithQueue.Should().BeFalse();
        settings.CheckForUpdatesOnStartup.Should().BeFalse();
        settings.DetectClipboardData.Should().BeTrue();
        settings.EnabledFormats.Should().Equal("PNG");
        settings.UpdateCheckInterval.Should().Be(UpdateCheckInterval.Weekly);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Read_FullFixture_ReturnsEveryValueExactly(bool withBom) {
        var settings = LegacyConfigReader.Read(FixturePath("full.cfg", withBom));

        settings.Language.Should().Be("System");
        settings.OutputFolder.Should().Be(@"C:\Data\My Pictures\Out");
        settings.SaveToSourceFolder.Should().BeFalse();
        settings.LastInputFolder.Should().Be(@"D:\In");
        settings.ConfirmExitWithQueue.Should().BeTrue();
        settings.CheckForUpdatesOnStartup.Should().BeTrue();
        settings.DetectClipboardData.Should().BeTrue();
        settings.EnabledFormats.Should().Equal("JPG", "PNG", "WEBP");
        settings.UpdateCheckInterval.Should().Be(UpdateCheckInterval.EveryThreeDays);
    }

    [Fact]
    public void Parse_ContentWithBom_ReadsTheFirstSection() {
        var content = File.ReadAllText(Path.Combine(FixtureFolder, "full.cfg"));

        var settings = LegacyConfigReader.Parse("\uFEFF" + content);

        settings.LastInputFolder.Should().Be(@"D:\In");
    }

    [Fact]
    public void Parse_LfLineEndings_ReadsEveryKey() {
        var content = File.ReadAllText(Path.Combine(FixtureFolder, "full.cfg")).ReplaceLineEndings("\n");

        var settings = LegacyConfigReader.Parse(content);

        settings.OutputFolder.Should().Be(@"C:\Data\My Pictures\Out");
        settings.UpdateCheckInterval.Should().Be(UpdateCheckInterval.EveryThreeDays);
    }

    [Fact]
    public void Read_FourKeyFile_KeepsDefaultsForMissingKeys() {
        var settings = LegacyConfigReader.Read(Path.Combine(FixtureFolder, "four-keys.cfg"));
        var defaults = new GeneralSettings();

        settings.Language.Should().Be("ru");
        settings.OutputFolder.Should().Be(@"D:\Converted");
        settings.LastInputFolder.Should().BeEmpty();
        settings.SaveToSourceFolder.Should().Be(defaults.SaveToSourceFolder);
        settings.ConfirmExitWithQueue.Should().Be(defaults.ConfirmExitWithQueue);
        settings.CheckForUpdatesOnStartup.Should().Be(defaults.CheckForUpdatesOnStartup);
        settings.DetectClipboardData.Should().Be(defaults.DetectClipboardData);
        settings.EnabledFormats.Should().BeEmpty();
        settings.UpdateCheckInterval.Should().Be(defaults.UpdateCheckInterval);
    }

    [Fact]
    public void Parse_EnabledFormatsWithBlankEntries_DropsThem() {
        var settings = LegacyConfigReader.Parse("[General]\r\nEnabledFormats=JPG, ,PNG,\r\n");

        settings.EnabledFormats.Should().Equal("JPG", "PNG");
    }

    [Fact]
    public void Parse_EmptyEnabledFormats_MeansAllFormats() {
        var settings = LegacyConfigReader.Parse("[General]\r\nEnabledFormats=\r\n");

        settings.EnabledFormats.Should().BeEmpty();
    }

    [Theory]
    [InlineData("True", true)]
    [InlineData("true", true)]
    [InlineData("yes", true)]
    [InlineData("1", true)]
    [InlineData("False", false)]
    [InlineData("false", false)]
    [InlineData("no", false)]
    [InlineData("0", false)]
    public void Parse_BoolSpellings_AreAccepted(string value, bool expected) {
        var settings = LegacyConfigReader.Parse($"[General]\r\nDetectClipboardData={value}\r\n");

        settings.DetectClipboardData.Should().Be(expected);
    }

    [Fact]
    public void Parse_KeyNames_MatchIgnoringCase() {
        var settings = LegacyConfigReader.Parse("[general]\r\nlastinputfolder=D:\\In\r\nUPDATECHECKINTERVAL=Monthly\r\n");

        settings.LastInputFolder.Should().Be(@"D:\In");
        settings.UpdateCheckInterval.Should().Be(UpdateCheckInterval.Monthly);
    }

    [Fact]
    public void Parse_InvalidBool_KeepsOnlyThatDefault() {
        var settings = LegacyConfigReader.Parse(
            "[General]\r\nConfirmExitWithQueue=maybe\r\nLastInputFolder=D:\\In\r\nDetectClipboardData=True\r\n");

        settings.ConfirmExitWithQueue.Should().BeTrue();
        settings.LastInputFolder.Should().Be(@"D:\In");
        settings.DetectClipboardData.Should().BeTrue();
    }

    [Theory]
    [InlineData("Hourly")]
    [InlineData("99")]
    [InlineData("1")]
    [InlineData("weekly")]
    [InlineData("")]
    public void Parse_InvalidEnum_KeepsOnlyThatDefault(string value) {
        var settings = LegacyConfigReader.Parse(
            $"[General]\r\nUpdateCheckInterval={value}\r\nLastInputFolder=D:\\In\r\n");

        settings.UpdateCheckInterval.Should().Be(UpdateCheckInterval.Daily);
        settings.LastInputFolder.Should().Be(@"D:\In");
    }

    [Theory]
    [InlineData("Klingon-Empire")]
    [InlineData("system")]
    [InlineData("")]
    public void Parse_InvalidLanguage_FallsBackToSystem(string value) {
        var settings = LegacyConfigReader.Parse($"[General]\r\nLanguage={value}\r\nLastInputFolder=D:\\In\r\n");

        settings.Language.Should().Be(App.SystemLanguageName);
        settings.LastInputFolder.Should().Be(@"D:\In");
    }

    [Fact]
    public void Parse_LineWithoutEquals_IsSkipped() {
        var settings = LegacyConfigReader.Parse(
            "[General]\r\nLastInputFolder=D:\\In\r\nthis line is broken\r\nDetectClipboardData=True\r\n");

        settings.LastInputFolder.Should().Be(@"D:\In");
        settings.DetectClipboardData.Should().BeTrue();
    }

    [Fact]
    public void Parse_UnclosedSectionHeader_IsSkippedWithoutThrowing() {
        var settings = LegacyConfigReader.Parse("[General\r\nLastInputFolder=D:\\In\r\n");

        settings.LastInputFolder.Should().BeEmpty();
        settings.Language.Should().Be(App.SystemLanguageName);
    }

    [Fact]
    public void Parse_KeysWithoutSection_AreIgnored() {
        var settings = LegacyConfigReader.Parse("LastInputFolder=D:\\In\r\nDetectClipboardData=True\r\n");

        settings.LastInputFolder.Should().BeEmpty();
        settings.DetectClipboardData.Should().BeFalse();
    }

    [Fact]
    public void Parse_KeysInOtherSections_AreIgnored() {
        var settings = LegacyConfigReader.Parse(
            "[Other]\r\nLastInputFolder=C:\\Wrong\r\n[General]\r\nDetectClipboardData=True\r\n[More]\r\nDetectClipboardData=False\r\n");

        settings.LastInputFolder.Should().BeEmpty();
        settings.DetectClipboardData.Should().BeTrue();
    }

    [Fact]
    public void Parse_UnknownKeys_AreIgnored() {
        var settings = LegacyConfigReader.Parse("[General]\r\nBrailleUiMode=True\r\nFoo=bar\r\nLastInputFolder=D:\\In\r\n");

        settings.LastInputFolder.Should().Be(@"D:\In");
    }

    [Fact]
    public void Parse_Comments_AreStrippedOutsideQuotes() {
        var settings = LegacyConfigReader.Parse(
            "# comment\r\n[General] ; trailing\r\nLastInputFolder=D:\\In # note\r\nOutputFolder=\"C:\\a;b\" ; note\r\n");

        settings.LastInputFolder.Should().Be(@"D:\In");
        settings.OutputFolder.Should().Be(@"C:\a;b");
    }

    [Theory]
    [InlineData("")]
    [InlineData("\uFEFF")]
    [InlineData("\r\n\r\n   \r\n")]
    public void Parse_EmptyContent_ReturnsDefaults(string content) {
        var settings = LegacyConfigReader.Parse(content);
        var defaults = new GeneralSettings();

        settings.Should().BeEquivalentTo(defaults);
    }

    /// <summary>
    /// The path of fixture <paramref name="fileName"/>, or of a copy in the temporary folder with a
    /// UTF-8 BOM in front when <paramref name="withBom"/> is set.
    /// </summary>
    private string FixturePath(string fileName, bool withBom) {
        var source = Path.Combine(FixtureFolder, fileName);

        if (!withBom) {
            return source;
        }

        var bytes = File.ReadAllBytes(source);
        bytes.Take(3).Should().NotEqual(Encoding.UTF8.Preamble.ToArray(), "the captured fixture has no BOM of its own");

        var copy = Path.Combine(_tempFolder, fileName);
        File.WriteAllBytes(copy, [.. Encoding.UTF8.Preamble, .. bytes]);

        return copy;
    }
}
