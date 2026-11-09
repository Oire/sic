using AwesomeAssertions;
using Oire.Sic.Models;
using Oire.Sic.Services;
using Oire.Sic.Utils.Enums;
using Xunit;

namespace Oire.Sic.Tests;

public sealed class PresetCapturerTests {
    [Fact]
    public void Capture_ResizeOff_HasNoSizesAndKeepsProportions() {
        var preset = PresetCapturer.Capture("WEBP", false, ResizeMode.Crop, "300", "200", DimensionBox.None);

        preset.Format.Should().Be("WEBP");
        preset.Name.Should().BeEmpty();
        preset.Width.Should().BeNull();
        preset.Height.Should().BeNull();
        preset.ResizeMode.Should().Be(ResizeMode.KeepProportions);
    }

    [Theory]
    [InlineData(DimensionBox.None)]
    [InlineData(DimensionBox.Width)]
    [InlineData(DimensionBox.Height)]
    [InlineData(DimensionBox.Both)]
    public void Capture_Crop_KeepsBothSizes(DimensionBox autoFilledBox) {
        var preset = PresetCapturer.Capture("PNG", true, ResizeMode.Crop, "128", "96", autoFilledBox);

        preset.ResizeMode.Should().Be(ResizeMode.Crop);
        preset.Width.Should().Be(128);
        preset.Height.Should().Be(96);
    }

    [Fact]
    public void Capture_KeepWithNothingFilledIn_KeepsWhatWasTyped() {
        var preset = PresetCapturer.Capture("WEBP", true, ResizeMode.KeepProportions, "300", "", DimensionBox.None);

        preset.ResizeMode.Should().Be(ResizeMode.KeepProportions);
        preset.Width.Should().Be(300);
        preset.Height.Should().BeNull();
    }

    [Fact]
    public void Capture_KeepWithHeightFilledIn_KeepsOnlyTheTypedWidth() {
        var preset = PresetCapturer.Capture(
            "WEBP", true, ResizeMode.KeepProportions, "300", "225", DimensionBox.Height);

        preset.Width.Should().Be(300);
        preset.Height.Should().BeNull();
        preset.ResizeMode.Should().Be(ResizeMode.KeepProportions);
    }

    [Fact]
    public void Capture_KeepWithWidthFilledIn_KeepsOnlyTheTypedHeight() {
        var preset = PresetCapturer.Capture(
            "JPG", true, ResizeMode.KeepProportions, "267", "200", DimensionBox.Width);

        preset.Width.Should().BeNull();
        preset.Height.Should().Be(200);
        preset.ResizeMode.Should().Be(ResizeMode.KeepProportions);
    }

    [Fact]
    public void Capture_KeepWithBothFilledIn_HasNoSizes() {
        var preset = PresetCapturer.Capture(
            "PNG", true, ResizeMode.KeepProportions, "4000", "3000", DimensionBox.Both);

        preset.Resizes.Should().BeFalse();
        preset.ResizeMode.Should().Be(ResizeMode.KeepProportions);
    }

    [Fact]
    public void Capture_BothTypedWithNoSelection_KeepsBoth() {
        var preset = PresetCapturer.Capture(
            "PNG", true, ResizeMode.KeepProportions, "800", "600", DimensionBox.None);

        preset.Width.Should().Be(800);
        preset.Height.Should().Be(600);
        preset.ResizeMode.Should().Be(ResizeMode.KeepProportions);
    }

    [Fact]
    public void Capture_OneBoxClearedWhileTheOtherHoldsATypedSize_KeepsIt() {
        var preset = PresetCapturer.Capture("PNG", true, ResizeMode.KeepProportions, "", "200", DimensionBox.None);

        preset.Width.Should().BeNull();
        preset.Height.Should().Be(200);
    }

    [Fact]
    public void Capture_OneBoxClearedWhileSicFilledTheOther_HasNoSizes() {
        // The user cleared the width after SIC! had filled both, so only the height is still SIC!'s.
        var preset = PresetCapturer.Capture(
            "PNG", true, ResizeMode.KeepProportions, "", "200", DimensionBox.Height);

        preset.Resizes.Should().BeFalse();
        preset.ResizeMode.Should().Be(ResizeMode.KeepProportions);
    }

    [Theory]
    [InlineData("abc", "200", null, 200)]
    [InlineData("300", "xyz", 300, null)]
    [InlineData("0", "65536", null, null)]
    [InlineData(" 300 ", "", 300, null)]
    public void Capture_UnparsableText_IsLeftOut(string widthText, string heightText, int? width, int? height) {
        var preset = PresetCapturer.Capture(
            "WEBP", true, ResizeMode.KeepProportions, widthText, heightText, DimensionBox.None);

        preset.Width.Should().Be(width);
        preset.Height.Should().Be(height);
    }

    [Fact]
    public void Capture_CropWithNoValidSize_StoresKeepProportions() {
        var preset = PresetCapturer.Capture("PNG", true, ResizeMode.Crop, "", "abc", DimensionBox.None);

        preset.Resizes.Should().BeFalse();
        preset.ResizeMode.Should().Be(ResizeMode.KeepProportions);
    }

    [Fact]
    public void Capture_ResizeOnWithBlankBoxes_StoresKeepProportions() {
        var preset = PresetCapturer.Capture("PNG", true, ResizeMode.KeepProportions, "", "", DimensionBox.None);

        preset.Resizes.Should().BeFalse();
        preset.ResizeMode.Should().Be(ResizeMode.KeepProportions);
    }
}
