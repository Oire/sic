using Oire.Sic.Utils;

namespace Oire.Sic.Tests;

/// <summary>
/// A base for tests in <see cref="StaticStateCollection"/>. <c>Localization.SetLanguage</c>
/// writes <see cref="System.Globalization.CultureInfo.DefaultThreadCurrentCulture"/> for the
/// whole process, so every test starts and ends in <c>en-US</c>, whatever the one before it
/// switched to. Derived classes still need <c>[Collection(StaticStateCollection.Name)]</c>:
/// xUnit doesn't inherit it.
/// </summary>
public abstract class StaticStateTestBase: IDisposable {
    protected StaticStateTestBase() {
        Localization.SetLanguage("en-US");
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) {
            Localization.SetLanguage("en-US");
        }
    }
}
