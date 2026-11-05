using System.Runtime.CompilerServices;

namespace Oire.Sic.Tests;

/// <summary>
/// Creates the <c>userdata</c> folder beside the test assembly before any test runs, so SIC!
/// starts as portable and <c>App.DataFolder</c> stays inside the test output. The build target
/// creates it too, but the folder may be gone by the time tests run (deleted to clear leftovers
/// before <c>dotnet test --no-build</c>, an IDE skipping MSBuild, a copied output folder), and
/// <c>App.IsPortable</c> is read only once.
/// </summary>
internal static class TestAssemblySetup {
    // CA2255 warns that a library's module initializer runs in someone else's process. This one
    // only ever runs inside the test host, which is exactly the point.
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void CreateUserDataFolder() {
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "userdata"));
    }
}
