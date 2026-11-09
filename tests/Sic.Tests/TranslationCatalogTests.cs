using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oire.WinForms.NativeControls;
using Xunit;

namespace Oire.Sic.Tests;

/// <summary>
/// Checks every <c>Sic.po</c> on disk, so a new language is covered as soon as its catalog
/// exists. These read the catalogs as text; no test here loads or asserts a translation.
/// </summary>
public sealed partial class TranslationCatalogTests {
    private static readonly string[] TopLevelMenu = ["&File", "&Edit", "&Convert", "&Help"];

    private static readonly string[] FileMenu =
        ["Add &Image...", "Add F&older...", "Add Image by &Link...", "&Settings...", "E&xit"];

    private static readonly string[] EditMenu = ["&Remove", "Remove &All"];

    private static readonly string[] ConvertMenu =
        ["Convert &Selected", "Convert &All", "Create Multi-size &ICO...", "&Fit to File Size...", "&Presets"];

    // Added by PresetMenuBuilder.AddTo. The saved presets listed above these have their ampersands
    // escaped, so they never take a mnemonic.
    private static readonly string[] PresetsMenu = ["No presets yet", "&Add Preset...", "&Manage Presets..."];

    private static readonly string[] HelpMenu =
        ["Read User &Manual", "Check for &Updates...", "&Donate...", "&About SIC!..."];

    // The texts with a mnemonic in each dialog's designer: a clash sends Alt+letter to the wrong
    // control. DialogLists_MatchDesigners fails when these and the designers drift apart.
    private static readonly string[] PresetDialog =
        ["&Name:", "Target &Format:", "Resi&ze", "Resize &Mode:", "&Width:", "&Height:", "&OK", "&Cancel"];

    private static readonly string[] ManagePresetsDialog = ["Pre&sets:", "&Add...", "&Edit...", "&Remove", "&Close"];

    // Every dialog whose mnemonics are tested, by the name of its form. To cover another dialog,
    // add its array here and an InlineData row to DialogLists_MatchDesigners.
    private static readonly Dictionary<string, string[]> DialogLists = new() {
        [nameof(PresetDialog)] = PresetDialog,
        [nameof(ManagePresetsDialog)] = ManagePresetsDialog,
    };

    public static TheoryData<string> Languages => [.. PoCatalog.Languages];

    // One set per level of MainWindow.BuildMenuSpec: a duplicate mnemonic within a level makes
    // NativeMenuSpec throw ArgumentException at startup. MenuLists_MatchBuildMenuSpec fails when
    // the arrays above and the menu drift apart.
    public static TheoryData<string, string[]> TopLevelMenuStrings => ForEveryLanguage(TopLevelMenu);

    public static TheoryData<string, string[]> FileMenuStrings => ForEveryLanguage(FileMenu);

    public static TheoryData<string, string[]> EditMenuStrings => ForEveryLanguage(EditMenu);

    public static TheoryData<string, string[]> ConvertMenuStrings => ForEveryLanguage(ConvertMenu);

    public static TheoryData<string, string[]> HelpMenuStrings => ForEveryLanguage(HelpMenu);

    public static TheoryData<string, string[]> PresetsMenuStrings => ForEveryLanguage(PresetsMenu);

    public static TheoryData<string, string[]> DialogStrings {
        get {
            var data = new TheoryData<string, string[]>();
            foreach (var msgIds in DialogLists.Values) {
                foreach (var language in PoCatalog.Languages) {
                    data.Add(language, msgIds);
                }
            }

            return data;
        }
    }

    [Fact]
    public void Languages_AreFoundOnDisk() {
        PoCatalog.Languages.Should().Contain(["de", "ru", "uk"]);
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Catalog_HasNoFuzzyEntries(string language) {
        var catalog = PoCatalog.Load(language);

        catalog.Entries.Should().NotBeEmpty();
        catalog.Entries.Where(entry => entry.IsFuzzy).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Catalog_HasNoFuzzyHeader(string language) {
        PoCatalog.Load(language).IsHeaderFuzzy.Should().BeFalse("msgfmt drops a fuzzy header, and its Plural-Forms with it");
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Catalog_DeclaresPluralForms(string language) {
        PoCatalog.Load(language).PluralFormCount.Should().HaveValue().And.BeGreaterThan(0);
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Catalog_HasNoEmptyTranslations(string language) {
        var catalog = PoCatalog.Load(language);

        catalog.Entries
            .Where(entry => entry.MsgStrs.Count == 0 || entry.MsgStrs.Any(text => text.Length == 0))
            .Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Catalog_GivesEveryPluralEntryOneFormPerPluralRule(string language) {
        var catalog = PoCatalog.Load(language);

        catalog.Entries
            .Where(entry => entry.IsPlural && entry.MsgStrs.Count != catalog.PluralFormCount)
            .Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void SingularEntries_KeepThePlaceholdersOfTheSourceText(string language) {
        var catalog = PoCatalog.Load(language);

        var mismatches = catalog.Entries
            .Where(entry => !entry.IsPlural)
            .Where(entry => !Placeholders(entry.MsgStrs[0]).SetEquals(Placeholders(entry.MsgId)))
            .Select(entry => $"{entry} -> \"{entry.MsgStrs[0]}\"");

        mismatches.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void PluralEntries_UseOnlyThePlaceholdersOfTheSourcePlural(string language) {
        var catalog = PoCatalog.Load(language);

        var mismatches = catalog.Entries
            .Where(entry => entry.IsPlural)
            .SelectMany(entry => entry.MsgStrs
                .Where(text => !Placeholders(text).IsSubsetOf(Placeholders(entry.MsgIdPlural!)))
                .Select(text => $"{entry} -> \"{text}\""));

        mismatches.Should().BeEmpty();
    }

    /// <summary>
    /// Every form after the first covers counts that need the number, such as 2 to 4 and 22 to 24
    /// in Russian and Ukrainian as well as the last form, so each keeps every placeholder.
    /// </summary>
    [Theory]
    [MemberData(nameof(Languages))]
    public void PluralEntries_KeepEveryPlaceholderInEveryFormAfterTheFirst(string language) {
        var catalog = PoCatalog.Load(language);

        var mismatches = catalog.Entries
            .Where(entry => entry.IsPlural)
            .SelectMany(entry => entry.MsgStrs
                .Skip(1)
                .Where(text => !Placeholders(text).SetEquals(Placeholders(entry.MsgIdPlural!)))
                .Select(text => $"{entry} -> \"{text}\""));

        mismatches.Should().BeEmpty();
    }

    /// <summary>
    /// The first form may drop a placeholder only when the English singular has none, as in
    /// "One file". With more than two forms it never may: there it also covers 21, 31 and so on.
    /// </summary>
    [Theory]
    [MemberData(nameof(Languages))]
    public void PluralEntries_KeepThePlaceholdersInTheFirstForm(string language) {
        var catalog = PoCatalog.Load(language);

        var mismatches = catalog.Entries
            .Where(entry => entry.IsPlural && entry.MsgStrs.Count > 0)
            .Where(entry => {
                var required = catalog.PluralFormCount > 2
                    ? Placeholders(entry.MsgIdPlural!)
                    : Placeholders(entry.MsgId);
                return !required.IsSubsetOf(Placeholders(entry.MsgStrs[0]));
            })
            .Select(entry => $"{entry} -> \"{entry.MsgStrs[0]}\"");

        mismatches.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(TopLevelMenuStrings))]
    [MemberData(nameof(FileMenuStrings))]
    [MemberData(nameof(EditMenuStrings))]
    [MemberData(nameof(ConvertMenuStrings))]
    [MemberData(nameof(HelpMenuStrings))]
    [MemberData(nameof(PresetsMenuStrings))]
    public void MenuLevel_HasNoDuplicateMnemonics(string language, string[] msgIds) {
        DuplicateMnemonics(Translate(language, msgIds)).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(DialogStrings))]
    public void Dialog_HasNoDuplicateMnemonics(string language, string[] msgIds) {
        DuplicateMnemonics(Translate(language, msgIds)).Should().BeEmpty();
    }

    [Fact]
    public void MenuLists_MatchBuildMenuSpec() {
        var source = File.ReadAllText(Path.Combine(PoCatalog.RepositoryRoot, "src", "Sic", "MainWindow.cs"));
        var start = source.IndexOf("NativeMenuSpec BuildMenuSpec()", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, "MainWindow.cs should still build its menu in BuildMenuSpec");
        var end = source.IndexOf("ApplyMenuState(spec);", start, StringComparison.Ordinal);
        end.Should().BeGreaterThan(start, "BuildMenuSpec should still end by applying the menu state");

        // Each AddMenu call opens a level; the .Add calls after it are that level's items.
        // PresetMenuBuilder.AddTo adds the Presets submenu to the level it is called in.
        var levels = new List<(string Title, List<string> Items)>();
        var presetsMenu = new List<string>();
        foreach (Match call in MenuCallPattern().Matches(source[start..end])) {
            if (call.Groups["call"].Value == "AddMenu") {
                levels.Add((call.Groups["text"].Value, []));
                continue;
            }

            levels.Should().NotBeEmpty("menu items are added inside a menu");
            if (call.Groups["builder"].Success) {
                var (title, items) = ReadPresetMenuBuilder();
                levels[^1].Items.Add(title);
                presetsMenu.AddRange(items);
            } else {
                levels[^1].Items.Add(call.Groups["text"].Value);
            }
        }

        levels.Select(level => level.Title).Should().Equal(TopLevelMenu);
        levels.Select(level => level.Items).Should().BeEquivalentTo(
            [FileMenu, EditMenu, ConvertMenu, HelpMenu],
            options => options.WithStrictOrdering());
        presetsMenu.Should().Equal(PresetsMenu);
    }

    [Theory]
    [InlineData("PresetDialog.Designer.cs", nameof(PresetDialog))]
    [InlineData("ManagePresetsDialog.Designer.cs", nameof(ManagePresetsDialog))]
    public void DialogLists_MatchDesigners(string designerFile, string listName) {
        var source = File.ReadAllText(Path.Combine(PoCatalog.RepositoryRoot, "src", "Sic", designerFile));
        DialogLists.Should().ContainKey(listName, "every dialog checked against its designer needs its list in DialogLists");
        var expected = DialogLists[listName];

        var texts = DesignerTextPattern().Matches(source)
            .Select(match => match.Groups["text"].Value)
            .Where(text => MenuTextFormatter.ExtractMnemonic(text) is not null);

        texts.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void EnglishMenusAndDialogs_HaveNoDuplicateMnemonics() {
        string[][] groups = [
            TopLevelMenu, FileMenu, EditMenu, ConvertMenu, HelpMenu, PresetsMenu, .. DialogLists.Values,
        ];

        foreach (var group in groups) {
            DuplicateMnemonics(group).Should().BeEmpty();
        }
    }

    private static TheoryData<string, string[]> ForEveryLanguage(string[] msgIds) {
        var data = new TheoryData<string, string[]>();
        foreach (var language in PoCatalog.Languages) {
            data.Add(language, msgIds);
        }

        return data;
    }

    /// <summary>
    /// The Presets submenu as <c>PresetMenuBuilder.AddTo</c> builds it: the submenu's title, and
    /// the items with translated text inside it.
    /// </summary>
    private static (string Title, List<string> Items) ReadPresetMenuBuilder() {
        var source = File.ReadAllText(
            Path.Combine(PoCatalog.RepositoryRoot, "src", "Sic", "Utils", "PresetMenuBuilder.cs"));
        var start = source.IndexOf("public static void AddTo(", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, "PresetMenuBuilder.cs should still build the submenu in AddTo");

        string? title = null;
        var items = new List<string>();
        foreach (Match call in MenuCallPattern().Matches(source[start..])) {
            if (call.Groups["call"].Value == "AddMenu") {
                title.Should().BeNull("AddTo should add a single submenu");
                title = call.Groups["text"].Value;
            } else if (call.Groups["call"].Success) {
                title.Should().NotBeNull("menu items are added inside a menu");
                items.Add(call.Groups["text"].Value);
            }
        }

        title.Should().NotBeNull("AddTo should add the Presets submenu");
        return (title!, items);
    }

    private static List<string> Translate(string language, string[] msgIds) {
        var catalog = PoCatalog.Load(language);

        return [.. msgIds.Select(msgId => {
            var entry = catalog.Find(msgId);
            entry.Should().NotBeNull($"the {language} catalog should translate \"{msgId}\"");
            return entry!.MsgStrs[0];
        })];
    }

    private static IEnumerable<string> DuplicateMnemonics(IEnumerable<string> labels) =>
        labels
            .Select(label => (Label: label, Mnemonic: MenuTextFormatter.ExtractMnemonic(label)))
            .Where(item => item.Mnemonic is not null)
            .GroupBy(item => item.Mnemonic)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(item => $"\"{item.Label}\""))}");

    /// <summary>The distinct <c>{0}</c>-style placeholders in a string, ignoring escaped braces.</summary>
    private static HashSet<string> Placeholders(string text) =>
        [.. PlaceholderPattern()
            .Matches(text.Replace("{{", string.Empty, StringComparison.Ordinal)
                .Replace("}}", string.Empty, StringComparison.Ordinal))
            .Select(match => match.Value)];

    [GeneratedRegex(@"\{\d+(?:,-?\d+)?(?::[^{}]*)?\}")]
    private static partial Regex PlaceholderPattern();

    /// <summary>
    /// The first <c>_("...")</c> argument of an <c>AddMenu</c> or <c>.Add</c> call, or a call to
    /// <c>PresetMenuBuilder.AddTo</c>.
    /// </summary>
    [GeneratedRegex(@"(?<call>AddMenu|\.Add)\(\s*_\(""(?<text>[^""]*)""\)|(?<builder>PresetMenuBuilder\.AddTo)\(")]
    private static partial Regex MenuCallPattern();

    /// <summary>A qualified <c>control.Text = "...";</c> assignment in a designer file.</summary>
    [GeneratedRegex(@"^\s*\w+\.Text = ""(?<text>[^""]*)"";", RegexOptions.Multiline)]
    private static partial Regex DesignerTextPattern();
}
