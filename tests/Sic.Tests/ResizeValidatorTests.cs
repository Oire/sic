using AwesomeAssertions;
using Oire.Sic.Models;
using Oire.Sic.Services;
using Oire.Sic.Utils.Enums;
using Xunit;

namespace Oire.Sic.Tests;

public class ResizeValidatorTests {
    [Theory]
    [InlineData("300", "", 300, null)]
    [InlineData("", "200", null, 200)]
    [InlineData("300", "200", 300, 200)]
    [InlineData(" 300 ", "\t200 ", 300, 200)]
    [InlineData("1", "", 1, null)]
    [InlineData("65535", "65535", 65535, 65535)]
    public void Validate_KeepProportions_AcceptsOneOrBothSizes(string widthText, string heightText, int? expectedWidth, int? expectedHeight) {
        var error = ResizeValidator.Validate(widthText, heightText, ResizeMode.KeepProportions, out var width, out var height);

        error.Should().Be(ResizeError.None);
        width.Should().Be(expectedWidth);
        height.Should().Be(expectedHeight);
    }

    [Theory]
    [InlineData("128", "64", 128, 64)]
    [InlineData("1", "1", 1, 1)]
    [InlineData("65535", "65535", 65535, 65535)]
    public void Validate_Crop_AcceptsBothSizes(string widthText, string heightText, int expectedWidth, int expectedHeight) {
        var error = ResizeValidator.Validate(widthText, heightText, ResizeMode.Crop, out var width, out var height);

        error.Should().Be(ResizeError.None);
        width.Should().Be(expectedWidth);
        height.Should().Be(expectedHeight);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("  ", "\t")]
    public void Validate_KeepProportions_BlankFields_AreMissingDimension(string widthText, string heightText) {
        var error = ResizeValidator.Validate(widthText, heightText, ResizeMode.KeepProportions, out var width, out var height);

        error.Should().Be(ResizeError.MissingDimension);
        width.Should().BeNull();
        height.Should().BeNull();
    }

    [Theory]
    [InlineData("0", "100", ResizeError.InvalidWidth)]
    [InlineData("65536", "100", ResizeError.InvalidWidth)]
    [InlineData("-5", "100", ResizeError.InvalidWidth)]
    [InlineData("abc", "100", ResizeError.InvalidWidth)]
    [InlineData("12.5", "100", ResizeError.InvalidWidth)]
    [InlineData("100", "0", ResizeError.InvalidHeight)]
    [InlineData("100", "65536", ResizeError.InvalidHeight)]
    [InlineData("100", "abc", ResizeError.InvalidHeight)]
    [InlineData("0", "", ResizeError.InvalidWidth)]
    [InlineData("", "65536", ResizeError.InvalidHeight)]
    public void Validate_KeepProportions_RejectsBadText_EvenWithTheOtherSizeValid(string widthText, string heightText, ResizeError expected) {
        var error = ResizeValidator.Validate(widthText, heightText, ResizeMode.KeepProportions, out var width, out var height);

        error.Should().Be(expected);
        width.Should().BeNull();
        height.Should().BeNull();
    }

    [Theory]
    [InlineData("", "100", ResizeError.CropNeedsWidth)]
    [InlineData("100", "", ResizeError.CropNeedsHeight)]
    [InlineData("", "", ResizeError.CropNeedsWidth)]
    [InlineData("0", "100", ResizeError.InvalidWidth)]
    [InlineData("100", "65536", ResizeError.InvalidHeight)]
    public void Validate_Crop_RejectsMissingOrBadSizes(string widthText, string heightText, ResizeError expected) {
        var error = ResizeValidator.Validate(widthText, heightText, ResizeMode.Crop, out var width, out var height);

        error.Should().Be(expected);
        width.Should().BeNull();
        height.Should().BeNull();
    }

    [Theory]
    [InlineData("abc", "1", ResizeMode.Crop, ResizeError.InvalidWidth)]
    [InlineData("", "abc", ResizeMode.Crop, ResizeError.CropNeedsWidth)]
    [InlineData("abc", "", ResizeMode.KeepProportions, ResizeError.InvalidWidth)]
    [InlineData("", "abc", ResizeMode.KeepProportions, ResizeError.InvalidHeight)]
    [InlineData("abc", "xyz", ResizeMode.KeepProportions, ResizeError.InvalidWidth)]
    public void Validate_ChecksWidthBeforeHeight(string widthText, string heightText, ResizeMode mode, ResizeError expected) {
        ResizeValidator.Validate(widthText, heightText, mode, out _, out _).Should().Be(expected);
    }

    [Theory]
    [InlineData("128x128", "128", "128")]
    [InlineData("128x", "128", "")]
    [InlineData("x128", "", "128")]
    [InlineData("128", "128", "")]
    [InlineData("128X128", "128", "128")]
    public void TryParseSpec_AcceptsTheCliForms(string spec, string expectedWidth, string expectedHeight) {
        ResizeValidator.TryParseSpec(spec, out var widthText, out var heightText).Should().BeTrue();
        widthText.Should().Be(expectedWidth);
        heightText.Should().Be(expectedHeight);
    }

    [Theory]
    [InlineData("1x2x3")]
    [InlineData("1xX3")]
    public void TryParseSpec_RejectsMoreThanOneSeparator(string spec) {
        ResizeValidator.TryParseSpec(spec, out _, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData(300, 4000, 3000, 225)]
    [InlineData(200, 300, 400, 267)]
    [InlineData(16, 4000, 100, 1)]
    [InlineData(1, 65535, 1, 1)]
    [InlineData(60000, 100, 200, 65535)]
    [InlineData(int.MaxValue, 1, int.MaxValue, 65535)]
    public void ScaleDimension_ScalesAndClamps(int target, int sourceFrom, int sourceTo, int expected) {
        ResizeValidator.ScaleDimension(target, sourceFrom, sourceTo).Should().Be(expected);
    }
}
