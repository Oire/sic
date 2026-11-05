using System.Security.AccessControl;
using System.Security.Principal;
using AwesomeAssertions;
using Oire.Sic.Utils;
using Xunit;

namespace Oire.Sic.Tests;

public sealed class FileHelperTests: IDisposable {
    private static readonly string[] PngOnly = ["*.png"];
    private static readonly string[] PngAndJpg = ["*.png", "*.jpg"];

    private readonly TestImages _images = new();

    private string Root => _images.Folder;

    public void Dispose() {
        _images.Dispose();
    }

    /// <summary>Creates an empty file at <paramref name="relativePath"/> under the root.</summary>
    private string Touch(string relativePath) {
        var path = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, []);
        return path;
    }

    [Fact]
    public void EnumerateImageFiles_TopFolderOnly_SkipsSubfolders() {
        var top = Touch("a.png");
        Touch(Path.Combine("sub", "b.png"));
        Touch("c.txt");

        var files = FileHelper.EnumerateImageFiles(Root, PngOnly, SearchOption.TopDirectoryOnly);

        files.Should().BeEquivalentTo([top]);
    }

    [Fact]
    public void EnumerateImageFiles_Recursive_IncludesSubfolders() {
        var top = Touch("a.png");
        var nested = Touch(Path.Combine("sub", "deeper", "b.png"));
        Touch(Path.Combine("sub", "c.txt"));

        var files = FileHelper.EnumerateImageFiles(Root, PngOnly, SearchOption.AllDirectories);

        files.Should().BeEquivalentTo([top, nested]);
    }

    [Fact]
    public void EnumerateImageFiles_SeveralExtensions_ReturnsEachInPatternOrder() {
        var jpg = Touch("a.jpg");
        var png = Touch("b.png");
        Touch("c.gif");

        var files = FileHelper.EnumerateImageFiles(Root, PngAndJpg, SearchOption.TopDirectoryOnly);

        files.Should().Equal(png, jpg);
    }

    [Fact]
    public void EnumerateImageFiles_SkipsAnInaccessibleFolder() {
        var visible = Touch("a.png");
        var lockedFolder = Path.Combine(Root, "locked");
        Touch(Path.Combine("locked", "hidden.png"));

        var user = WindowsIdentity.GetCurrent().User!;
        var deny = new FileSystemAccessRule(user, FileSystemRights.ListDirectory, AccessControlType.Deny);
        var directory = new DirectoryInfo(lockedFolder);
        var security = directory.GetAccessControl();
        security.AddAccessRule(deny);
        directory.SetAccessControl(security);

        try {
            // Proves the Deny ACE took effect, so the test can't pass for the wrong reason.
            var listLocked = () => Directory.GetFiles(lockedFolder);
            listLocked.Should().Throw<UnauthorizedAccessException>();

            var files = FileHelper.EnumerateImageFiles(Root, PngOnly, SearchOption.AllDirectories).ToList();

            files.Should().BeEquivalentTo([visible]);
        } finally {
            security = directory.GetAccessControl();
            security.RemoveAccessRule(deny);
            directory.SetAccessControl(security);
        }
    }

    [Fact]
    public void IsCloudPlaceholder_IsFalseForANormalFile() {
        var path = Touch("a.png");

        FileHelper.IsCloudPlaceholder(path).Should().BeFalse();
    }

    [Fact]
    public void IsCloudPlaceholder_IsTrueForAMissingFile() {
        var path = Path.Combine(Root, "missing.png");

        FileHelper.IsCloudPlaceholder(path).Should().BeTrue();
    }
}
