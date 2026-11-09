using AwesomeAssertions;
using Oire.Sic.Models;
using Oire.Sic.Services;
using Oire.Sic.Utils.Enums;
using Xunit;

namespace Oire.Sic.Tests;

// The messages come from _(), so these tests run in the static-state collection, in en-US.
[Collection(StaticStateCollection.Name)]
public sealed class ResizeErrorDescriberTests: StaticStateTestBase {
    private const string CropWidth = "Crop mode requires a valid width (1–65535).";
    private const string CropHeight = "Crop mode requires a valid height (1–65535).";
    private const string Width = "The width must be a whole number from 1 to 65535, or left empty.";
    private const string Height = "The height must be a whole number from 1 to 65535, or left empty.";
    private const string Missing = "Please enter at least one valid dimension (1–65535).";

    [Theory]
    [InlineData(ResizeError.InvalidWidth, ResizeMode.KeepProportions, Width, "Invalid width", DimensionBox.Width)]
    [InlineData(ResizeError.InvalidHeight, ResizeMode.KeepProportions, Height, "Invalid height", DimensionBox.Height)]
    [InlineData(ResizeError.MissingDimension, ResizeMode.KeepProportions, Missing, "Invalid dimensions", DimensionBox.Width)]
    [InlineData(ResizeError.CropNeedsWidth, ResizeMode.KeepProportions, Missing, "Invalid dimensions", DimensionBox.Width)]
    [InlineData(ResizeError.CropNeedsHeight, ResizeMode.KeepProportions, Missing, "Invalid dimensions", DimensionBox.Width)]
    [InlineData(ResizeError.InvalidWidth, ResizeMode.Crop, CropWidth, "Invalid width", DimensionBox.Width)]
    [InlineData(ResizeError.InvalidHeight, ResizeMode.Crop, CropHeight, "Invalid height", DimensionBox.Height)]
    [InlineData(ResizeError.MissingDimension, ResizeMode.Crop, Missing, "Invalid dimensions", DimensionBox.Width)]
    [InlineData(ResizeError.CropNeedsWidth, ResizeMode.Crop, CropWidth, "Invalid width", DimensionBox.Width)]
    [InlineData(ResizeError.CropNeedsHeight, ResizeMode.Crop, CropHeight, "Invalid height", DimensionBox.Height)]
    public void Describe_GivesTheMessageCaptionAndBox(
        ResizeError error, ResizeMode mode, string message, string caption, DimensionBox focus) {
        ResizeErrorDescriber.Describe(error, mode).Should().Be((message, caption, focus));
    }

    [Fact]
    public void Describe_CoversEveryErrorInEveryMode() {
        foreach (var error in Enum.GetValues<ResizeError>().Where(error => error != ResizeError.None)) {
            foreach (var mode in Enum.GetValues<ResizeMode>()) {
                var (message, caption, focus) = ResizeErrorDescriber.Describe(error, mode);

                message.Should().NotBeNullOrWhiteSpace();
                caption.Should().NotBeNullOrWhiteSpace();
                focus.Should().BeOneOf(DimensionBox.Width, DimensionBox.Height);
            }
        }
    }

    [Theory]
    [InlineData(ResizeMode.KeepProportions)]
    [InlineData(ResizeMode.Crop)]
    public void Describe_None_Throws(ResizeMode mode) {
        var describe = () => ResizeErrorDescriber.Describe(ResizeError.None, mode);

        describe.Should().Throw<ArgumentOutOfRangeException>();
    }
}
