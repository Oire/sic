using AwesomeAssertions;
using Oire.Sic.Utils.Constants;
using Xunit;

namespace Oire.Sic.Tests;

/// <summary>
/// Guards the test isolation: the test project's after-build target creates a <c>userdata</c>
/// folder beside the test assembly, which makes SIC! run as portable and keeps every path under
/// <see cref="App.DataFolder"/> inside the test output. If this ever stops being true, tests
/// would read and write the user's real <c>%APPDATA%\Oire\Sic</c> folder.
/// </summary>
public class TestIsolationTests {
    [Fact]
    public void App_IsPortable() {
        App.IsPortable.Should().BeTrue();
    }

    [Fact]
    public void DataFolder_IsUnderTheTestOutputFolder() {
        var outputFolder = Path.GetFullPath(AppContext.BaseDirectory);

        Path.GetFullPath(App.DataFolder).Should().StartWith(outputFolder);
        App.DataFolder.Should().NotStartWith(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        );
    }

    [Fact]
    public void DefaultOutputFolder_IsUnderTheTestOutputFolder() {
        Path.GetFullPath(App.DefaultOutputFolder).Should().StartWith(Path.GetFullPath(AppContext.BaseDirectory));
    }
}
