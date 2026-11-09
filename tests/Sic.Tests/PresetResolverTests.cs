using AwesomeAssertions;
using Oire.Sic.Models;
using Oire.Sic.Services;
using Xunit;

namespace Oire.Sic.Tests;

public sealed class PresetResolverTests {
    private static readonly ConversionPreset Blog = new() { Name = "Blog", Format = "WEBP", Width = 300 };
    private static readonly ConversionPreset CyrillicBlog = new() { Name = "Блог", Format = "JPG", Height = 200 };
    private static readonly ConversionPreset Demarche = new() { Name = "Démarche administrative", Format = "PNG" };
    private static readonly ConversionPreset Avatar = new() {
        Name = "Avatar",
        Format = "PNG",
        ResizeMode = ResizeMode.Crop,
        Width = 128,
        Height = 128,
    };

    private static readonly List<ConversionPreset> Presets = [Blog, CyrillicBlog, Demarche, Avatar];

    #region Find

    [Fact]
    public void Find_ExactName_FindsThePreset() {
        PresetResolver.Find(Presets, "Blog").Should().BeSameAs(Blog);
    }

    [Theory]
    [InlineData("blog")]
    [InlineData("BLOG")]
    [InlineData("  bLoG  ")]
    public void Find_DifferentCaseOrSpaces_FindsThePreset(string name) {
        PresetResolver.Find(Presets, name).Should().BeSameAs(Blog);
    }

    [Fact]
    public void Find_LowerCaseCyrillic_FindsThePreset() {
        PresetResolver.Find(Presets, "блог").Should().BeSameAs(CyrillicBlog);
    }

    [Fact]
    public void Find_UpperCaseWithDiacritics_FindsThePreset() {
        PresetResolver.Find(Presets, "DÉMARCHE ADMINISTRATIVE").Should().BeSameAs(Demarche);
    }

    [Fact]
    public void Find_DecomposedName_FindsThePresetStoredComposed() {
        PresetResolver.Find(Presets, "Démarche administrative").Should().BeSameAs(Demarche);
    }

    [Theory]
    [InlineData("Banner")]
    [InlineData("")]
    [InlineData("Blo")]
    public void Find_UnknownName_GivesNull(string name) {
        PresetResolver.Find(Presets, name).Should().BeNull();
    }

    [Fact]
    public void Find_EmptyList_GivesNull() {
        PresetResolver.Find([], "Blog").Should().BeNull();
    }

    #endregion

    #region ResolveResize

    [Fact]
    public void ResolveResize_NoPresetAndNoResize_GivesNoSizes() {
        PresetResolver.ResolveResize(null, false, null, null, false)
            .Should().Be(((int?)null, (int?)null, ResizeMode.KeepProportions));
    }

    [Fact]
    public void ResolveResize_NoPresetWithResize_GivesTheExplicitSizes() {
        PresetResolver.ResolveResize(null, true, 64, 32, false)
            .Should().Be(((int?)64, (int?)32, ResizeMode.KeepProportions));
    }

    [Fact]
    public void ResolveResize_NoPresetWithResizeAndCrop_GivesCrop() {
        PresetResolver.ResolveResize(null, true, 64, 32, true)
            .Should().Be(((int?)64, (int?)32, ResizeMode.Crop));
    }

    [Fact]
    public void ResolveResize_PresetOnly_GivesThePresetSizesAndMode() {
        PresetResolver.ResolveResize(Blog, false, null, null, false)
            .Should().Be(((int?)300, (int?)null, ResizeMode.KeepProportions));
        PresetResolver.ResolveResize(Avatar, false, null, null, false)
            .Should().Be(((int?)128, (int?)128, ResizeMode.Crop));
    }

    [Fact]
    public void ResolveResize_ExplicitResize_ReplacesThePresetSizesAndMode() {
        PresetResolver.ResolveResize(Avatar, true, 50, null, false)
            .Should().Be(((int?)50, (int?)null, ResizeMode.KeepProportions));
    }

    [Fact]
    public void ResolveResize_ExplicitResizeWithCrop_TakesTheModeFromCrop() {
        PresetResolver.ResolveResize(Blog, true, 50, 40, true)
            .Should().Be(((int?)50, (int?)40, ResizeMode.Crop));
    }

    [Fact]
    public void ResolveResize_CropAloneOverAPresetWithBothSizes_KeepsTheSizesAndCrops() {
        var fitInside = new ConversionPreset { Name = "Box", Format = "JPG", Width = 800, Height = 600 };

        PresetResolver.ResolveResize(fitInside, false, null, null, true)
            .Should().Be(((int?)800, (int?)600, ResizeMode.Crop));
    }

    [Fact]
    public void ResolveResize_PresetWithoutResize_GivesNoSizes() {
        PresetResolver.ResolveResize(Demarche, false, null, null, false)
            .Should().Be(((int?)null, (int?)null, ResizeMode.KeepProportions));
    }

    #endregion
}
