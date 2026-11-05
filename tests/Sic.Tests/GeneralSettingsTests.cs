using AwesomeAssertions;
using Oire.Sic.Models;
using Oire.Sic.Utils.Enums;
using Xunit;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic.Tests;

public class GeneralSettingsTests {
    [Fact]
    public void Defaults_MatchAFreshInstall() {
        var settings = new GeneralSettings();

        settings.Language.Should().Be("System");
        settings.OutputFolder.Should().Be(App.DefaultOutputFolder);
        settings.SaveToSourceFolder.Should().BeFalse();
        settings.LastInputFolder.Should().BeEmpty();
        settings.ConfirmExitWithQueue.Should().BeTrue();
        settings.CheckForUpdatesOnStartup.Should().BeTrue();
        settings.DetectClipboardData.Should().BeFalse();
        settings.EnabledFormats.Should().BeEmpty();
        settings.UpdateCheckInterval.Should().Be(UpdateCheckInterval.Daily);
    }

    [Fact]
    public void AppConfig_StartsWithDefaultGeneralSettings() {
        var config = new AppConfig();

        config.General.Should().NotBeNull();
        config.General.Language.Should().Be("System");
    }

    [Fact]
    public void GetEnabledFormatKeys_EmptyList_ReturnsNothing() {
        var settings = new GeneralSettings();

        settings.GetEnabledFormatKeys().Should().BeEmpty();
    }

    [Fact]
    public void GetEnabledFormatKeys_TrimsEntries() {
        var settings = new GeneralSettings { EnabledFormats = [" JPG", "PNG ", " WEBP "] };

        settings.GetEnabledFormatKeys().Should().Equal("JPG", "PNG", "WEBP");
    }

    [Fact]
    public void GetEnabledFormatKeys_SkipsNullAndBlankEntries() {
        var settings = new GeneralSettings { EnabledFormats = [null!, " ", "JPG"] };

        settings.GetEnabledFormatKeys().Should().Equal("JPG");
    }
}
