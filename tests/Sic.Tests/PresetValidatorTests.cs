using AwesomeAssertions;
using Oire.Sic.Models;
using Oire.Sic.Services;
using Oire.Sic.Utils.Enums;
using Oire.WinForms.NativeControls;
using Xunit;

namespace Oire.Sic.Tests;

public sealed class PresetValidatorTests {
    private const string ComposedName = "Démarche";
    private const string DecomposedName = "Démarche";

    #region ValidateName

    [Theory]
    [InlineData("Blog")]
    [InlineData("  Blog  ")]
    [InlineData(" Blog ")]
    [InlineData("Блог")]
    [InlineData("Démarche administrative")]
    [InlineData("בלוג")]
    [InlineData("R&D")]
    public void ValidateName_UsableName_IsValid(string name) {
        PresetValidator.ValidateName(name, []).Should().Be(PresetNameResult.Valid);
    }

    [Fact]
    public void ValidateName_SixtyFourCharacters_IsValid() {
        PresetValidator.ValidateName(new string('a', 64), []).Should().Be(PresetNameResult.Valid);
    }

    [Fact]
    public void ValidateName_SixtyFourCharactersWithSpacesAround_IsValid() {
        PresetValidator.ValidateName("  " + new string('a', 64) + "  ", []).Should().Be(PresetNameResult.Valid);
    }

    [Fact]
    public void ValidateName_SixtyFiveCharacters_IsTooLong() {
        PresetValidator.ValidateName(new string('a', 65), []).Should().Be(PresetNameResult.TooLong);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateName_EmptyName_IsEmpty(string? name) {
        PresetValidator.ValidateName(name, []).Should().Be(PresetNameResult.Empty);
    }

    [Theory]
    [InlineData("Blog\tWide")]
    [InlineData("Blog\nWide")]
    [InlineData("Blog\u0000")]
    [InlineData("Blog\u001B")]
    [InlineData("Blog\u007F")]
    [InlineData("Blog\u0085Wide")]
    public void ValidateName_ControlCharacter_IsInvalid(string name) {
        PresetValidator.ValidateName(name, []).Should().Be(PresetNameResult.InvalidCharacters);
    }

    [Theory]
    [InlineData("Blog", "Blog")]
    [InlineData("blog", "Blog")]
    [InlineData("BLOG", "Blog")]
    [InlineData(" Blog ", "Blog")]
    [InlineData("блог", "Блог")]
    [InlineData(ComposedName, DecomposedName)]
    [InlineData(DecomposedName, ComposedName)]
    [InlineData("DÉMARCHE", DecomposedName)]
    public void ValidateName_TakenName_IsDuplicate(string name, string taken) {
        PresetValidator.ValidateName(name, ["Avatar", taken]).Should().Be(PresetNameResult.Duplicate);
    }

    [Fact]
    public void ValidateName_DifferentName_IsNotDuplicate() {
        PresetValidator.ValidateName("Blog", ["Avatar", "Banner"]).Should().Be(PresetNameResult.Valid);
    }

    #endregion

    #region CleanName

    [Fact]
    public void CleanName_TrimsAndComposes() {
        PresetValidator.CleanName("  " + DecomposedName + " ").Should().Be(ComposedName);
    }

    #endregion

    #region Normalize

    [Fact]
    public void Normalize_Null_GivesNothing() {
        var (presets, dropped) = PresetValidator.Normalize(null);

        presets.Should().BeEmpty();
        dropped.Should().Be(0);
    }

    [Fact]
    public void Normalize_UsablePresets_AreKeptInOrder() {
        var (presets, dropped) = PresetValidator.Normalize([
            Preset("Blog", "WEBP", width: 300),
            Preset("Avatar", "PNG", ResizeMode.Crop, 128, 128),
            Preset("Archive", "TIFF"),
        ]);

        dropped.Should().Be(0);
        presets.Select(preset => preset.Name).Should().Equal("Blog", "Avatar", "Archive");
        presets[1].Should().BeEquivalentTo(Preset("Avatar", "PNG", ResizeMode.Crop, 128, 128));
    }

    public static TheoryData<string, ConversionPreset?> UnusablePresets() => new() {
        { "null entry", null },
        { "null name", new ConversionPreset { Name = null!, Format = "PNG" } },
        { "null format", new ConversionPreset { Name = "Bad", Format = null! } },
        { "empty name", Preset("  ", "PNG") },
        { "name too long", Preset(new string('a', 65), "PNG") },
        { "control character", Preset("Bad\tName", "PNG") },
        { "duplicate name", Preset("BLOG", "PNG") },
        { "unknown format", Preset("Bad", "PSD") },
        { "input-only format", Preset("Bad", "HEIC") },
        { "empty format", Preset("Bad", "") },
        { "width 0", Preset("Bad", "PNG", width: 0) },
        { "negative height", Preset("Bad", "PNG", height: -1) },
        { "width above the limit", Preset("Bad", "PNG", width: 65536) },
        { "height above the limit", Preset("Bad", "PNG", height: 65536) },
        { "undefined mode", Preset("Bad", "PNG", (ResizeMode)5, 100, 100) },
        { "crop with only a width", Preset("Bad", "PNG", ResizeMode.Crop, width: 100) },
        { "crop with only a height", Preset("Bad", "PNG", ResizeMode.Crop, height: 100) },
    };

    [Theory]
    [MemberData(nameof(UnusablePresets))]
    public void Normalize_UnusablePreset_IsDroppedAndCounted(string reason, ConversionPreset? unusable) {
        var (presets, dropped) = PresetValidator.Normalize([
            Preset("Blog", "WEBP", width: 300),
            unusable,
            Preset("Avatar", "PNG", ResizeMode.Crop, 128, 128),
        ]);

        dropped.Should().Be(1, reason);
        presets.Select(preset => preset.Name).Should().Equal("Blog", "Avatar");
    }

    [Fact]
    public void Normalize_SeveralUnusable_AreAllCounted() {
        var (presets, dropped) = PresetValidator.Normalize([
            null,
            Preset("Blog", "WEBP"),
            Preset("blog", "PNG"),
            Preset("Bad", "PSD"),
        ]);

        dropped.Should().Be(3);
        presets.Should().ContainSingle().Which.Name.Should().Be("Blog");
    }

    [Fact]
    public void Normalize_DuplicateDifferingByComposition_IsDropped() {
        var (presets, dropped) = PresetValidator.Normalize([Preset(ComposedName, "PNG"), Preset(DecomposedName, "JPG")]);

        dropped.Should().Be(1);
        presets.Should().ContainSingle().Which.Format.Should().Be("PNG");
    }

    [Fact]
    public void Normalize_NameWithSpacesAround_IsStoredCleaned() {
        var (presets, _) = PresetValidator.Normalize([Preset("  " + DecomposedName + "  ", "PNG")]);

        presets.Should().ContainSingle().Which.Name.Should().Be(ComposedName);
    }

    [Theory]
    [InlineData("webp", "WEBP")]
    [InlineData(" png ", "PNG")]
    [InlineData("Avif", "AVIF")]
    [InlineData("JPEG", "JPG")]
    [InlineData("jpeg", "JPG")]
    [InlineData("TIF", "TIFF")]
    [InlineData("tif", "TIFF")]
    [InlineData("ico", "ICO")]
    public void Normalize_Format_IsUpperCasedAndMapped(string format, string expected) {
        var (presets, dropped) = PresetValidator.Normalize([Preset("Blog", format)]);

        dropped.Should().Be(0);
        presets.Should().ContainSingle().Which.Format.Should().Be(expected);
    }

    [Fact]
    public void Normalize_CropWithNoSizes_IsStoredAsKeepProportions() {
        var (presets, dropped) = PresetValidator.Normalize([Preset("Plain", "PNG", ResizeMode.Crop)]);

        dropped.Should().Be(0);
        var preset = presets.Should().ContainSingle().Subject;
        preset.ResizeMode.Should().Be(ResizeMode.KeepProportions);
        preset.Resizes.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, null)]
    [InlineData(null, 65535)]
    [InlineData(65535, 1)]
    public void Normalize_SizesAtTheLimits_AreKept(int? width, int? height) {
        var (presets, dropped) = PresetValidator.Normalize([Preset("Edge", "PNG", width: width, height: height)]);

        dropped.Should().Be(0);
        presets.Should().ContainSingle().Which.Should().BeEquivalentTo(Preset("Edge", "PNG", width: width, height: height));
    }

    [Fact]
    public void Normalize_DoesNotChangeTheGivenPresets() {
        var original = Preset(" blog ", "webp");

        PresetValidator.Normalize([original]);

        original.Name.Should().Be(" blog ");
        original.Format.Should().Be("webp");
    }

    #endregion

    #region EscapeAmpersands

    [Fact]
    public void EscapeAmpersands_DoublesEveryAmpersand() {
        PresetValidator.EscapeAmpersands("R&D").Should().Be("R&&D");
        PresetValidator.EscapeAmpersands("A&B&C").Should().Be("A&&B&&C");
    }

    [Fact]
    public void EscapeAmpersands_LeavesNoMnemonic() {
        MenuTextFormatter.ExtractMnemonic(PresetValidator.EscapeAmpersands("R&D")).Should().BeNull();
    }

    [Fact]
    public void EscapeAmpersands_NameWithoutAmpersand_IsUnchanged() {
        PresetValidator.EscapeAmpersands("Blog").Should().Be("Blog");
    }

    #endregion

    private static ConversionPreset Preset(
        string name,
        string format,
        ResizeMode mode = ResizeMode.KeepProportions,
        int? width = null,
        int? height = null) => new() {
            Name = name,
            Format = format,
            ResizeMode = mode,
            Width = width,
            Height = height,
        };
}
