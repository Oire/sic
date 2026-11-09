using AwesomeAssertions;
using Oire.Sic.Utils;
using Oire.Sic.Utils.Enums;
using Xunit;

namespace Oire.Sic.Tests;

public sealed class ResizeFieldTrackerTests {
    [Fact]
    public void New_IsUnpinnedWithNothingAutoFilled() {
        var tracker = new ResizeFieldTracker();

        tracker.SizesPinned.Should().BeFalse();
        tracker.AutoFilledBox.Should().Be(DimensionBox.None);
    }

    [Theory]
    [InlineData(DimensionBox.None, DimensionBox.Width, DimensionBox.Width)]
    [InlineData(DimensionBox.None, DimensionBox.Height, DimensionBox.Height)]
    [InlineData(DimensionBox.None, DimensionBox.Both, DimensionBox.Both)]
    [InlineData(DimensionBox.Width, DimensionBox.Width, DimensionBox.Width)]
    [InlineData(DimensionBox.Width, DimensionBox.Height, DimensionBox.Both)]
    [InlineData(DimensionBox.Height, DimensionBox.Width, DimensionBox.Both)]
    [InlineData(DimensionBox.Height, DimensionBox.Height, DimensionBox.Height)]
    [InlineData(DimensionBox.Both, DimensionBox.Width, DimensionBox.Both)]
    [InlineData(DimensionBox.Both, DimensionBox.Height, DimensionBox.Both)]
    public void AutoFilled_AddsTheBoxes(DimensionBox before, DimensionBox filled, DimensionBox expected) {
        var tracker = TrackerWith(before);

        tracker.AutoFilled(filled);

        tracker.AutoFilledBox.Should().Be(expected);
    }

    [Theory]
    [InlineData(DimensionBox.None, DimensionBox.Width, DimensionBox.None)]
    [InlineData(DimensionBox.None, DimensionBox.Height, DimensionBox.None)]
    [InlineData(DimensionBox.Width, DimensionBox.Width, DimensionBox.None)]
    [InlineData(DimensionBox.Width, DimensionBox.Height, DimensionBox.Width)]
    [InlineData(DimensionBox.Height, DimensionBox.Width, DimensionBox.Height)]
    [InlineData(DimensionBox.Height, DimensionBox.Height, DimensionBox.None)]
    [InlineData(DimensionBox.Both, DimensionBox.Width, DimensionBox.Height)]
    [InlineData(DimensionBox.Both, DimensionBox.Height, DimensionBox.Width)]
    public void UserEdited_RemovesOnlyThatBox(DimensionBox before, DimensionBox edited, DimensionBox expected) {
        var tracker = TrackerWith(before);

        tracker.UserEdited(edited);

        tracker.AutoFilledBox.Should().Be(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PresetApplied_PinsWhenItResizesAndClearsTheAutoFilledBoxes(bool resizes) {
        var tracker = TrackerWith(DimensionBox.Both);

        tracker.PresetApplied(resizes);

        tracker.SizesPinned.Should().Be(resizes);
        tracker.AutoFilledBox.Should().Be(DimensionBox.None);
    }

    [Theory]
    [InlineData(DimensionBox.Width)]
    [InlineData(DimensionBox.Height)]
    public void UserEdited_Unpins(DimensionBox edited) {
        var tracker = new ResizeFieldTracker();
        tracker.PresetApplied(resizes: true);

        tracker.UserEdited(edited);

        tracker.SizesPinned.Should().BeFalse();
    }

    [Fact]
    public void ResizeSettingsChanged_UnpinsAndKeepsTheAutoFilledBoxes() {
        var tracker = new ResizeFieldTracker();
        tracker.PresetApplied(resizes: true);
        tracker.AutoFilled(DimensionBox.Height);

        tracker.ResizeSettingsChanged();

        tracker.SizesPinned.Should().BeFalse();
        tracker.AutoFilledBox.Should().Be(DimensionBox.Height);
    }

    [Fact]
    public void AutoFilled_KeepsThePin() {
        var tracker = new ResizeFieldTracker();
        tracker.PresetApplied(resizes: true);

        tracker.AutoFilled(DimensionBox.Height);

        tracker.SizesPinned.Should().BeTrue();
    }

    [Fact]
    public void TypeWidthAutoFillHeightThenClearWidth_LeavesOnlyTheHeightAutoFilled() {
        // The main window's sequence: typing a width makes SIC! fill in the height, and clearing
        // the width again is another edit by the user.
        var tracker = new ResizeFieldTracker();
        tracker.PresetApplied(resizes: true);

        tracker.UserEdited(DimensionBox.Width);
        tracker.AutoFilled(DimensionBox.Height);
        tracker.UserEdited(DimensionBox.Width);

        tracker.SizesPinned.Should().BeFalse();
        tracker.AutoFilledBox.Should().Be(DimensionBox.Height);
    }

    [Fact]
    public void SelectionFillThenTypeHeight_LeavesTheWidthAutoFilled() {
        // Ticking Resize fills both boxes; typing a height makes it the user's, and SIC! fills
        // the width from it again.
        var tracker = new ResizeFieldTracker();

        tracker.AutoFilled(DimensionBox.Both);
        tracker.UserEdited(DimensionBox.Height);
        tracker.AutoFilled(DimensionBox.Width);

        tracker.AutoFilledBox.Should().Be(DimensionBox.Width);
    }

    private static ResizeFieldTracker TrackerWith(DimensionBox autoFilled) {
        var tracker = new ResizeFieldTracker();
        tracker.AutoFilled(autoFilled);
        return tracker;
    }
}
