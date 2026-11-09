using AwesomeAssertions;
using Oire.Sic.Models;
using Oire.Sic.Utils;
using Oire.WinForms.NativeControls;
using Xunit;

namespace Oire.Sic.Tests;

// The menu labels come from _(), so these tests run in the static-state collection, in en-US.
[Collection(StaticStateCollection.Name)]
public sealed class PresetMenuBuilderTests: StaticStateTestBase {
    // Unescaped, R&D and &Drafts both take D, and Web &Avatar takes A beside &Add Preset...
    private static readonly string[] ClashingNames = ["R&D", "&Drafts", "Web &Avatar"];

    [Fact]
    public void AddTo_PresetsWithAmpersands_PassesValidation() {
        var spec = BuildSpec([.. ClashingNames.Select(Preset)]);

        var validate = () => MenuSpecValidator.Validate(spec);

        validate.Should().NotThrow();
    }

    [Fact]
    public void Validate_TheSameNamesUnescaped_Throws() {
        // The control for the test above: without the escaping, these names do clash.
        var spec = new NativeMenuSpec();
        spec.AddMenu("&Presets", presets => {
            foreach (var name in ClashingNames) {
                presets.Add(name, () => { });
            }

            presets.Add("&Add Preset...", () => { });
            presets.Add("&Manage Presets...", () => { });
        });

        var validate = () => MenuSpecValidator.Validate(spec);

        validate.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddTo_Presets_ListsEachByEscapedNameThenAddAndManage() {
        var presets = PresetsMenu(BuildSpec([Preset("R&D"), Preset("Blog")]));

        presets.Text.Should().Be("&Presets");
        presets.Children!.Select(item => item.IsSeparator ? "-" : item.Text)
            .Should().Equal("R&&D", "Blog", "-", "&Add Preset...", "&Manage Presets...");
        presets.Children!.Where(item => !item.IsSeparator).Should().OnlyContain(item => item.IsEnabled);
    }

    [Fact]
    public void AddTo_AppendsSeparatorAndPresetsAfterExistingItems() {
        var spec = new NativeMenuSpec();
        spec.AddMenu("&Convert", convert => {
            convert.Add("Convert &All", () => { });
            PresetMenuBuilder.AddTo(convert, [], _ => { }, () => { }, () => { });
        });

        var children = spec.Items[0].Children!;
        children.Should().HaveCount(3);
        children[1].IsSeparator.Should().BeTrue();
        children[2].IsSubMenu.Should().BeTrue();
        children[2].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void AddTo_NoPresets_ShowsOneDisabledRow() {
        var spec = BuildSpec([]);
        var presets = PresetsMenu(spec);

        var placeholder = presets.Children![0];
        placeholder.Text.Should().Be("No presets yet");
        placeholder.IsEnabled.Should().BeFalse();
        presets.Children!.Where(item => !item.IsSeparator && !item.IsEnabled).Should().ContainSingle();
        presets.Children!.Select(item => item.IsSeparator ? "-" : item.Text)
            .Should().Equal("No presets yet", "-", "&Add Preset...", "&Manage Presets...");

        var validate = () => MenuSpecValidator.Validate(spec);
        validate.Should().NotThrow();
    }

    [Fact]
    public void AddTo_TwoHundredPresets_PassesValidation() {
        var spec = BuildSpec([.. Enumerable.Range(1, 200).Select(i => Preset($"Preset & {i}"))]);

        var validate = () => MenuSpecValidator.Validate(spec);

        validate.Should().NotThrow();
        PresetsMenu(spec).Children.Should().HaveCount(203);
    }

    [Fact]
    public void AddTo_ChoosingItems_CallsTheMatchingCallbacks() {
        var blog = Preset("Blog");
        var banner = Preset("Banner");
        ConversionPreset? applied = null;
        var added = 0;
        var managed = 0;

        var spec = new NativeMenuSpec();
        spec.AddMenu("&Convert", convert =>
            PresetMenuBuilder.AddTo(convert, [blog, banner], preset => applied = preset, () => added++, () => managed++));
        var presets = spec.Items[0].Children![^1];

        presets.Children![1].OnClick!();
        presets.Children![3].OnClick!();
        presets.Children![4].OnClick!();

        applied.Should().BeSameAs(banner);
        added.Should().Be(1);
        managed.Should().Be(1);
    }

    private static NativeMenuSpec BuildSpec(List<ConversionPreset> presets) {
        var spec = new NativeMenuSpec();
        spec.AddMenu("&Convert", convert => {
            convert.Add("Convert &Selected", () => { });
            convert.Add("Convert &All", () => { });
            convert.AddSeparator();
            convert.Add("Create Multi-size &ICO...", () => { });
            convert.Add("&Fit to File Size...", () => { });
            PresetMenuBuilder.AddTo(convert, presets, _ => { }, () => { }, () => { });
        });
        return spec;
    }

    private static NativeMenuItemSpec PresetsMenu(NativeMenuSpec spec) => spec.Items[0].Children![^1];

    private static ConversionPreset Preset(string name) => new() { Name = name, Format = "WEBP" };
}
