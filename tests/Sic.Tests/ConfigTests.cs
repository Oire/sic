using AwesomeAssertions;
using Oire.Sic.Utils;
using Xunit;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic.Tests;

/// <summary>
/// <see cref="Config"/> over the real data folder, which under test is the <c>userdata</c> folder
/// in the test output. Every test loads with <c>isGui: false</c>, so errors go to stderr, which is
/// captured, and never to a message box.
/// </summary>
[Collection(StaticStateCollection.Name)]
public sealed class ConfigTests: StaticStateTestBase {
    private readonly TextWriter _originalError = Console.Error;
    private readonly StringWriter _error = new();

    public ConfigTests() {
        TestDataFolder.Clear();
        Console.SetError(_error);
    }

    protected override void Dispose(bool disposing) {
        if (disposing) {
            Console.SetError(_originalError);
            _error.Dispose();
            TestDataFolder.Clear();
        }

        base.Dispose(disposing);
    }

    private static string ConfigPath => Path.Combine(App.DataFolder, App.ConfigFileName);

    private static FileStream HoldOpen(string path) =>
        new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

    [Fact]
    public void Save_WhenTheFileIsHeldOpen_WritesToStderrAndReturns() {
        Config.Load(isGui: false);
        _error.ToString().Should().BeEmpty();
        Config.General.Language = "de";

        using (HoldOpen(ConfigPath)) {
            var save = () => Config.Save();

            save.Should().NotThrow();
        }

        _error.ToString().Should().StartWith("Unable to save configuration: ");
    }

    [Fact]
    public void Load_WhenTheFileCantBeRead_SaysChangesWontBeSaved() {
        File.WriteAllText(ConfigPath, """{ "general": { "language": "de" } }""");

        using (HoldOpen(ConfigPath)) {
            Config.Load(isGui: false);
        }

        _error.ToString().Should().Contain(ConfigPath).And.Contain("changes won't be saved this session");
    }

    [Fact]
    public void Save_WhenSavesAreBlocked_LeavesTheFileUntouched() {
        File.WriteAllText(ConfigPath, """{ "general": { "language": "de" } }""");
        var originalBytes = File.ReadAllBytes(ConfigPath);

        using (HoldOpen(ConfigPath)) {
            Config.Load(isGui: false);
        }

        var loadMessage = _error.ToString();
        Config.General.Language.Should().Be(App.SystemLanguageName);
        Config.General.DetectClipboardData = true;

        Config.Save();

        File.ReadAllBytes(ConfigPath).Should().Equal(originalBytes);
        _error.ToString().Should().Be(loadMessage);
        Directory.EnumerateFiles(App.DataFolder, App.ConfigFileName + "*").Should().ContainSingle();
    }

    [Fact]
    public void Save_AfterANormalLoad_WritesTheSettings() {
        Config.Load(isGui: false);
        Config.General.Language = "de";

        Config.Save();

        Config.Load(isGui: false);
        Config.General.Language.Should().Be("de");
        _error.ToString().Should().BeEmpty();
    }
}
