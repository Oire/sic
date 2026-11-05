using AwesomeAssertions;
using Oire.Sic.Utils;
using Xunit;

namespace Oire.Sic.Tests;

/// <summary>
/// <see cref="SingleInstanceGuard"/> with folders unique to each test, so tests running in
/// parallel never share a mutex. The folders are never created: the guard only uses their paths.
/// </summary>
public sealed class SingleInstanceGuardTests {
    private static string NewFolder() =>
        Path.Combine(Path.GetTempPath(), "SicGuardTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void TryAcquire_SameFolderTwice_SecondReturnsNull() {
        var folder = NewFolder();

        using var first = SingleInstanceGuard.TryAcquire(folder);
        using var second = SingleInstanceGuard.TryAcquire(folder);

        first.Should().NotBeNull();
        second.Should().BeNull();
    }

    [Fact]
    public void TryAcquire_SameFolderWithDifferentCaseAndTrailingBackslash_ReturnsNull() {
        var folder = NewFolder();

        using var first = SingleInstanceGuard.TryAcquire(folder);
        using var second = SingleInstanceGuard.TryAcquire(folder.ToUpperInvariant() + @"\");

        first.Should().NotBeNull();
        second.Should().BeNull();
    }

    [Fact]
    public void TryAcquire_DifferentFolder_Succeeds() {
        using var first = SingleInstanceGuard.TryAcquire(NewFolder());
        using var second = SingleInstanceGuard.TryAcquire(NewFolder());

        first.Should().NotBeNull();
        second.Should().NotBeNull();
    }

    [Fact]
    public void TryAcquire_AfterDispose_SucceedsAgain() {
        var folder = NewFolder();

        var first = SingleInstanceGuard.TryAcquire(folder);
        first.Should().NotBeNull();
        first!.Dispose();

        using var second = SingleInstanceGuard.TryAcquire(folder);
        second.Should().NotBeNull();
    }

    [Fact]
    public void Dispose_Twice_DoesNotThrow() {
        var guard = SingleInstanceGuard.TryAcquire(NewFolder());
        guard.Should().NotBeNull();

        guard!.Dispose();
        var disposeAgain = guard.Dispose;

        disposeAgain.Should().NotThrow();
    }

    [Fact]
    public void Dispose_FromAnotherThread_LetsTheNextAcquireSucceed() {
        var folder = NewFolder();
        var guard = SingleInstanceGuard.TryAcquire(folder);
        guard.Should().NotBeNull();

        Exception? error = null;
        var thread = new Thread(() => {
            try {
                guard!.Dispose();
            } catch (Exception ex) {
                error = ex;
            }
        });
        thread.Start();
        thread.Join();

        error.Should().BeNull();
        using var next = SingleInstanceGuard.TryAcquire(folder);
        next.Should().NotBeNull();
    }

    [Fact]
    public void GetActivationMessage_SameFolder_ReturnsTheSameNonZeroValue() {
        var folder = NewFolder();

        var first = SingleInstanceGuard.GetActivationMessage(folder);
        var second = SingleInstanceGuard.GetActivationMessage(folder);

        first.Should().NotBe(0);
        second.Should().Be(first);
    }

    [Fact]
    public void GetActivationMessage_DifferentFolders_ReturnDifferentValues() {
        var first = SingleInstanceGuard.GetActivationMessage(NewFolder());
        var second = SingleInstanceGuard.GetActivationMessage(NewFolder());

        first.Should().NotBe(0);
        second.Should().NotBe(0);
        second.Should().NotBe(first);
    }

    [Fact]
    public void GetInstanceKey_IsStableAndSixteenHexDigits() {
        var folder = NewFolder();

        var key = SingleInstanceGuard.GetInstanceKey(folder);

        key.Should().HaveLength(16);
        key.Should().MatchRegex("^[0-9A-F]{16}$");
        SingleInstanceGuard.GetInstanceKey(folder).Should().Be(key);
    }

    [Fact]
    public void GetInstanceKey_IgnoresCaseAndTrailingBackslash() {
        var folder = NewFolder();

        var key = SingleInstanceGuard.GetInstanceKey(folder);

        SingleInstanceGuard.GetInstanceKey(folder.ToUpperInvariant()).Should().Be(key);
        SingleInstanceGuard.GetInstanceKey(folder.ToLowerInvariant() + @"\").Should().Be(key);
    }

    [Fact]
    public void GetInstanceKey_DifferentFolders_Differ() {
        SingleInstanceGuard.GetInstanceKey(NewFolder())
            .Should().NotBe(SingleInstanceGuard.GetInstanceKey(NewFolder()));
    }
}
