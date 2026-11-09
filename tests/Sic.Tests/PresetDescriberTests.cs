using AwesomeAssertions;
using Oire.Sic.Models;
using Oire.Sic.Services;
using Xunit;

namespace Oire.Sic.Tests;

// The summaries come from _(), so these tests run in the static-state collection, in en-US.
[Collection(StaticStateCollection.Name)]
public sealed class PresetDescriberTests: StaticStateTestBase {
    [Fact]
    public void Describe_NoResize_GivesTheFormatAlone() {
        var preset = new ConversionPreset { Name = "Plain", Format = "WEBP" };

        PresetDescriber.Describe(preset).Should().Be("WEBP");
    }

    [Fact]
    public void Describe_WidthOnly_GivesTheWidth() {
        var preset = new ConversionPreset { Name = "Blog", Format = "WEBP", Width = 300 };

        PresetDescriber.Describe(preset).Should().Be("WEBP, width 300");
    }

    [Fact]
    public void Describe_HeightOnly_GivesTheHeight() {
        var preset = new ConversionPreset { Name = "Strip", Format = "WEBP", Height = 200 };

        PresetDescriber.Describe(preset).Should().Be("WEBP, height 200");
    }

    [Fact]
    public void Describe_KeepWithBothSizes_GivesTheBox() {
        var preset = new ConversionPreset { Name = "Screen", Format = "WEBP", Width = 800, Height = 600 };

        PresetDescriber.Describe(preset).Should().Be("WEBP, fit inside 800x600");
    }

    [Fact]
    public void Describe_Crop_GivesTheCropSize() {
        var preset = new ConversionPreset {
            Name = "Avatar",
            Format = "PNG",
            ResizeMode = ResizeMode.Crop,
            Width = 128,
            Height = 128,
        };

        PresetDescriber.Describe(preset).Should().Be("PNG, crop to 128x128");
    }

    [Fact]
    public void DescribeWithName_PutsTheNameFirst() {
        var preset = new ConversionPreset { Name = "Blog", Format = "WEBP", Width = 300 };

        PresetDescriber.DescribeWithName(preset).Should().Be("Blog: WEBP, width 300");
    }
}
