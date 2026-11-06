using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Oire.Sic.Models;
using Oire.Sic.Utils;
using Oire.Sic.Utils.Enums;
using Xunit;
using App = Oire.Sic.Utils.Constants.App;

namespace Oire.Sic.Tests;

/// <summary>
/// <see cref="ConfigSerializer"/> builds its messages with <c>_()</c>, so these tests run in the
/// static state collection. Every test works in its own temporary folder, never in the data folder.
/// </summary>
[Collection(StaticStateCollection.Name)]
public sealed partial class ConfigSerializerTests: StaticStateTestBase {
    private const string CustomFolder = @"C:\Out";

    /// <summary>A value for every setting, none of them the default, as raw JSON.</summary>
    private static readonly (string Key, string Raw)[] ValidValues = [
        ("language", "\"de\""),
        ("outputFolder", "\"C:\\\\Out\""),
        ("saveToSourceFolder", "true"),
        ("lastInputFolder", "\"D:\\\\In\""),
        ("confirmExitWithQueue", "false"),
        ("checkForUpdatesOnStartup", "false"),
        ("detectClipboardData", "true"),
        ("enabledFormats", "[\"JPG\", \"PNG\"]"),
        ("updateCheckInterval", "\"Weekly\""),
    ];

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "Sic.Tests", Guid.NewGuid().ToString("N"));

    public ConfigSerializerTests() {
        Directory.CreateDirectory(_folder);
    }

    protected override void Dispose(bool disposing) {
        if (disposing && Directory.Exists(_folder)) {
            Directory.Delete(_folder, recursive: true);
        }

        base.Dispose(disposing);
    }

    private string JsonPath => Path.Combine(_folder, App.ConfigFileName);

    private string LegacyPath => Path.Combine(_folder, App.LegacyConfigFileName);

    private string BadPath => JsonPath + ".bad";

    [GeneratedRegex(@"^sic\.json\.bad\.\d{8}-\d{6}(-\d+)?$")]
    private static partial Regex RotatedBackupName();

    #region Loading

    [Fact]
    public void Load_ValidFile_ReadsEverySetting() {
        WriteJson(BuildJson());

        var result = Load();

        result.Config.General.Should().BeEquivalentTo(ValidSettings());
        ShouldBeSilent(result);
        Backups().Should().BeEmpty();
    }

    [Fact]
    public void Load_MissingGeneral_UsesDefaultsSilently() {
        WriteJson("{}");

        var result = Load();

        result.Config.General.Should().BeEquivalentTo(new GeneralSettings());
        ShouldBeSilent(result);
        Backups().Should().BeEmpty();
    }

    [Fact]
    public void Load_NullGeneral_UsesDefaultsSilently() {
        WriteJson("""{ "general": null }""");

        var result = Load();

        result.Config.General.Should().BeEquivalentTo(new GeneralSettings());
        ShouldBeSilent(result);
    }

    [Theory]
    [InlineData("enabledFormats")]
    [InlineData("language")]
    [InlineData("lastInputFolder")]
    [InlineData("outputFolder")]
    public void Load_NullValue_BecomesTheDefaultWithoutCountingAsBad(string key) {
        WriteJson(BuildJson(key, "null"));

        var result = Load();

        result.Config.General.Should().BeEquivalentTo(ValidSettingsWithDefault(key));
        ShouldBeSilent(result);
        Backups().Should().BeEmpty();
    }

    [Theory]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    public void Load_BlankOutputFolder_BecomesTheDefaultFolder(string raw) {
        WriteJson(BuildJson("outputFolder", raw));

        var result = Load();

        result.Config.General.OutputFolder.Should().Be(App.DefaultOutputFolder);
        ShouldBeSilent(result);
    }

    [Fact]
    public void Load_HandEditedFileWithCommentsAndTrailingCommas_ReadsIt() {
        WriteJson("""
            // Edited by hand
            {
              /* the general section */
              "general": {
                "language": "de", // German
                "enabledFormats": ["JPG", "PNG",],
                "updateCheckInterval": "Monthly",
              },
            }
            """);

        var result = Load();

        result.Config.General.Language.Should().Be("de");
        result.Config.General.EnabledFormats.Should().Equal("JPG", "PNG");
        result.Config.General.UpdateCheckInterval.Should().Be(UpdateCheckInterval.Monthly);
        ShouldBeSilent(result);
    }

    [Fact]
    public void Load_NamesInAnotherCase_AreMatched() {
        WriteJson("""{ "General": { "OutputFolder": "C:\\Out", "LANGUAGE": "de" } }""");

        var result = Load();

        result.Config.General.OutputFolder.Should().Be(CustomFolder);
        result.Config.General.Language.Should().Be("de");
        ShouldBeSilent(result);
    }

    [Fact]
    public void Load_FileWithUtf8Bom_ReadsIt() {
        File.WriteAllBytes(JsonPath, [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(BuildJson())]);

        var result = Load();

        result.Config.General.Should().BeEquivalentTo(ValidSettings());
        ShouldBeSilent(result);
    }

    [Fact]
    public void Load_UnknownSettingsAndSections_AreIgnored() {
        WriteJson("""{ "general": { "language": "de", "brailleUiMode": 3 }, "other": [1, 2] }""");

        var result = Load();

        result.Config.General.Language.Should().Be("de");
        ShouldBeSilent(result);
        Backups().Should().BeEmpty();
    }

    #endregion

    #region Shape

    [Theory]
    [InlineData("[]")]
    [InlineData("5")]
    [InlineData("null")]
    [InlineData("\"general\"")]
    public void Load_RootThatIsNotAnObject_TakesTheCorruptFilePath(string json) {
        WriteJson(json);

        var result = Load();

        result.Config.General.Should().BeEquivalentTo(new GeneralSettings());
        result.LoadError.Should().Contain("sic.json.bad");
        result.SavesBlocked.Should().BeFalse();
        File.ReadAllText(BadPath).Should().Be(json);
    }

    [Theory]
    [InlineData("""{"\uD800":0}""")]
    [InlineData("""{ "general": { "\uDC00": true } }""")]
    public void Load_NameWithAnUnpairedSurrogate_TakesTheCorruptFilePath(string json) {
        WriteJson(json);

        var result = Load();

        result.Config.General.Should().BeEquivalentTo(new GeneralSettings());
        result.LoadError.Should().Contain("sic.json.bad");
        result.SavesBlocked.Should().BeFalse();
        File.ReadAllText(BadPath).Should().Be(json);
        Load().LoadError.Should().BeNull();
    }

    [Fact]
    public void Load_ValueWithAnUnpairedSurrogate_DoesNotStopTheLoad() {
        WriteJson("""{ "general": { "language": "\uD800", "detectClipboardData": true } }""");

        var result = Load();

        result.Config.General.Language.Should().Be(new GeneralSettings().Language);
        result.LoadError.Should().NotBeNull();
    }

    [Fact]
    public void Load_GeneralThatIsNotAnObject_KeepsDefaultsAndCountsAsBad() {
        WriteJson("""{ "general": [] }""");

        var result = Load();

        result.Config.General.Should().BeEquivalentTo(new GeneralSettings());
        result.LoadError.Should().Contain("1 setting").And.Contain("sic.json.bad");
        File.Exists(BadPath).Should().BeTrue();
    }

    [Fact]
    public void Load_NullAndBlankFormatKeys_AreDropped() {
        WriteJson("""{ "general": { "enabledFormats": [null, " ", "JPG"] } }""");

        var result = Load();

        result.Config.General.EnabledFormats.Should().Equal("JPG");
        ShouldBeSilent(result);
        Backups().Should().BeEmpty();
    }

    #endregion

    #region Encoding

    [Fact]
    public void Load_FileInALegacyCodePage_TakesTheCorruptFilePathAndLosesNothing() {
        var bytes = Encoding.Latin1.GetBytes("""{ "general": { "outputFolder": "C:\\Café" } }""");
        File.WriteAllBytes(JsonPath, bytes);

        var result = Load();

        result.Config.General.Should().BeEquivalentTo(new GeneralSettings());
        result.LoadError.Should().Contain("sic.json.bad");
        File.ReadAllBytes(BadPath).Should().Equal(bytes);
    }

    #endregion

    #region Per-value recovery

    [Theory]
    [InlineData("updateCheckInterval", "\"Hourly\"")]
    [InlineData("updateCheckInterval", "99")]
    [InlineData("updateCheckInterval", "\"Weekly, Never\"")]
    [InlineData("updateCheckInterval", "\"Never,Monthly\"")]
    [InlineData("confirmExitWithQueue", "5")]
    [InlineData("confirmExitWithQueue", "null")]
    [InlineData("enabledFormats", "\"JPG,PNG\"")]
    [InlineData("language", "\"not a language\"")]
    [InlineData("outputFolder", "42")]
    public void Load_OneBadValue_KeepsItsDefaultAndEverythingElse(string key, string raw) {
        var json = BuildJson(key, raw);
        WriteJson(json);

        var result = Load();

        result.Config.General.Should().BeEquivalentTo(ValidSettingsWithDefault(key));
        result.LoadError.Should().Contain("1 setting").And.Contain("sic.json.bad");
        result.SaveError.Should().BeNull();
        result.SavesBlocked.Should().BeFalse();
        File.ReadAllText(BadPath).Should().Be(json);
    }

    [Fact]
    public void Load_TwoBadValues_NamesTheCount() {
        WriteJson("""{ "general": { "language": "de", "detectClipboardData": "yes", "updateCheckInterval": 1 } }""");

        var result = Load();

        result.Config.General.Language.Should().Be("de");
        result.Config.General.DetectClipboardData.Should().BeFalse();
        result.Config.General.UpdateCheckInterval.Should().Be(UpdateCheckInterval.Daily);
        result.LoadError.Should().Contain("2 settings");
    }

    [Fact]
    public void Load_CombinedEnumNamesBesideAnotherBadValue_RepairsTheFile() {
        WriteJson("""{ "general": { "language": "de", "detectClipboardData": "yes", "updateCheckInterval": "Weekly, Never" } }""");

        var result = Load();

        result.LoadError.Should().Contain("2 settings");
        result.SaveError.Should().BeNull();
        result.Config.General.UpdateCheckInterval.Should().Be(UpdateCheckInterval.Daily);

        var next = Load();
        ShouldBeSilent(next);
        next.Config.General.Language.Should().Be("de");
        next.Config.General.UpdateCheckInterval.Should().Be(UpdateCheckInterval.Daily);
    }

    [Fact]
    public void TrySave_ValueTheSerializerCannotWrite_ReturnsAnErrorAndKeepsTheFile() {
        WriteJson(BuildJson());
        var config = new AppConfig { General = { UpdateCheckInterval = (UpdateCheckInterval)6 } };

        var error = ConfigSerializer.TrySave(JsonPath, config);

        error.Should().StartWith("Unable to save configuration: ");
        File.ReadAllText(JsonPath).Should().Be(BuildJson());
        TempFiles().Should().BeEmpty();
    }

    [Fact]
    public void Load_FileHeldOpenForAMoment_IsReadOnARetry() {
        WriteJson(BuildJson());
        var held = new FileStream(JsonPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var release = new Thread(() => {
            Thread.Sleep(100);
            held.Dispose();
        });
        release.Start();

        var result = Load();

        release.Join();
        ShouldBeSilent(result);
        result.Config.General.Should().BeEquivalentTo(ValidSettings());
    }

    #endregion

    #region Repair

    [Fact]
    public void Load_AfterARecovery_TheFileIsRepairedAndTheNextLoadIsSilent() {
        WriteJson(BuildJson("updateCheckInterval", "\"Hourly\""));

        Load().LoadError.Should().NotBeNull();

        var saved = Load();

        saved.Config.General.Should().BeEquivalentTo(ValidSettingsWithDefault("updateCheckInterval"));
        ShouldBeSilent(saved);
        Backups().Should().ContainSingle();
    }

    [Fact]
    public void Load_RepairThatCannotBeWritten_IsReportedAsASaveError() {
        var json = BuildJson("updateCheckInterval", "\"Hourly\"");
        WriteJson(json);

        ConfigLoadResult result;

        // Readers are allowed, but nothing may replace the file.
        using (new FileStream(JsonPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            result = Load();
        }

        result.LoadError.Should().Contain("1 setting");
        result.SaveError.Should().NotBeNull();
        result.SavesBlocked.Should().BeFalse();
        result.Config.General.Should().BeEquivalentTo(ValidSettingsWithDefault("updateCheckInterval"));
        File.ReadAllText(JsonPath).Should().Be(json);
        TempFiles().Should().BeEmpty();
    }

    [Fact]
    public void Load_RecoveriesInQuickSuccession_KeepEveryBackup() {
        string[] corrupt = ["{", "{{", "[["];

        foreach (var json in corrupt) {
            WriteJson(json);
            Load().LoadError.Should().Contain("sic.json.bad");
        }

        var backups = Backups();
        backups.Should().HaveCount(3);
        backups.Select(File.ReadAllText).Should().BeEquivalentTo(corrupt);
    }

    #endregion

    #region Round trips and the saved file

    [Theory]
    [InlineData(@"C:\Фото\Блог")]
    [InlineData(@"C:\תמונות\יצוא")]
    public void Save_ThenLoad_KeepsEverySettingAndWritesReadableText(string outputFolder) {
        var config = new AppConfig { General = ValidSettings() };
        config.General.OutputFolder = outputFolder;

        ConfigSerializer.Save(JsonPath, config);
        var result = Load();

        result.Config.General.Should().BeEquivalentTo(config.General);
        ShouldBeSilent(result);
        var text = File.ReadAllText(JsonPath);
        text.Should().Contain(outputFolder.Replace(@"\", @"\\", StringComparison.Ordinal));
        text.Should().NotContain(@"\u");
    }

    [Fact]
    public void Save_WritesOnePropertyPerLineAsUtf8WithoutBom() {
        ConfigSerializer.Save(JsonPath, new AppConfig { General = ValidSettings() });

        var bytes = File.ReadAllBytes(JsonPath);
        bytes.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });

        var lines = Encoding.UTF8.GetString(bytes).Split('\n').Select(line => line.Trim()).ToArray();
        lines[0].Should().Be("{");
        lines.Should().Contain("\"general\": {");

        foreach (var (key, _) in ValidValues) {
            lines.Should().ContainSingle(line => line.StartsWith($"\"{key}\":", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Save_DoesNotChangeTheConfigItIsGiven() {
        var config = new AppConfig();

        ConfigSerializer.Save(JsonPath, config);

        config.General.OutputFolder.Should().Be(App.DefaultOutputFolder);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Save_DefaultOutputFolder_IsWrittenAsEmptyAndLoadsAsTheDefault(bool upperCase, bool trailingSeparator) {
        var folder = upperCase ? App.DefaultOutputFolder.ToUpperInvariant() : App.DefaultOutputFolder;
        var config = new AppConfig();
        config.General.OutputFolder = trailingSeparator ? folder + @"\" : folder;

        ConfigSerializer.Save(JsonPath, config);

        File.ReadAllText(JsonPath).Should().Contain("\"outputFolder\": \"\"");
        Load().Config.General.OutputFolder.Should().Be(App.DefaultOutputFolder);
    }

    [Fact]
    public void Save_CustomOutputFolder_IsWrittenAsItIs() {
        var config = new AppConfig();
        config.General.OutputFolder = CustomFolder;

        ConfigSerializer.Save(JsonPath, config);

        File.ReadAllText(JsonPath).Should().Contain("\"outputFolder\": \"C:\\\\Out\"");
        Load().Config.General.OutputFolder.Should().Be(CustomFolder);
    }

    [Fact]
    public void Save_FailedSave_LeavesThePreviousFileAndNoTemporaryFile() {
        ConfigSerializer.Save(JsonPath, new AppConfig());
        var before = File.ReadAllBytes(JsonPath);
        var config = new AppConfig { General = ValidSettings() };

        using (new FileStream(JsonPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            var save = () => ConfigSerializer.Save(JsonPath, config);
            save.Should().Throw<Exception>()
                .Which.Should().Match<Exception>(ex => ex is IOException || ex is UnauthorizedAccessException);
        }

        File.ReadAllBytes(JsonPath).Should().Equal(before);
        TempFiles().Should().BeEmpty();
    }

    [Fact]
    public void Save_MissingFolder_IsCreated() {
        var path = Path.Combine(_folder, "nested", App.ConfigFileName);

        ConfigSerializer.Save(path, new AppConfig());

        File.Exists(path).Should().BeTrue();
    }

    #endregion

    #region Migration

    [Fact]
    public void Load_LegacyFile_IsMigratedAndRenamed() {
        WriteLegacy("Language=de", @"OutputFolder=""C:\Data\My Pictures\Out""", "EnabledFormats=JPG,PNG,WEBP",
            "UpdateCheckInterval=EveryThreeDays", "ConfirmExitWithQueue=False");

        var result = Load();

        ShouldBeSilent(result);
        result.Config.General.Language.Should().Be("de");
        result.Config.General.OutputFolder.Should().Be(@"C:\Data\My Pictures\Out");
        result.Config.General.EnabledFormats.Should().Equal("JPG", "PNG", "WEBP");
        result.Config.General.UpdateCheckInterval.Should().Be(UpdateCheckInterval.EveryThreeDays);
        result.Config.General.ConfirmExitWithQueue.Should().BeFalse();
        File.Exists(LegacyPath).Should().BeFalse();
        File.Exists(LegacyPath + ".bak").Should().BeTrue();
        Load().Config.General.Should().BeEquivalentTo(result.Config.General);
    }

    [Fact]
    public void Load_LegacyFile_OverwritesAnOlderLegacyBackup() {
        File.WriteAllText(LegacyPath + ".bak", "older");
        WriteLegacy("Language=de");

        Load();

        File.ReadAllText(LegacyPath + ".bak").Should().Contain("Language=de");
    }

    [Fact]
    public void Load_LegacyDefaultOutputFolder_IsSavedAsEmpty() {
        WriteLegacy($"OutputFolder={App.DefaultOutputFolder}");

        var result = Load();

        result.Config.General.OutputFolder.Should().Be(App.DefaultOutputFolder);
        File.ReadAllText(JsonPath).Should().Contain("\"outputFolder\": \"\"");
    }

    [Fact]
    public void Load_LegacyBlankOutputFolder_BecomesTheDefaultFolder() {
        WriteLegacy("OutputFolder=", "Language=de");

        Load().Config.General.OutputFolder.Should().Be(App.DefaultOutputFolder);
    }

    [Theory]
    [InlineData(@"E:\Old\Sic\userdata\Converted")]
    [InlineData(@"E:\Old\Sic\userdata\Converted\")]
    [InlineData(@"e:\old\sic\USERDATA\converted")]
    public void Load_LegacyPortableOutputFolderFromElsewhere_BecomesTheDefaultWhenPortable(string folder) {
        WriteLegacy($"OutputFolder={folder}");

        var result = Load(isPortable: true);

        result.Config.General.OutputFolder.Should().Be(App.DefaultOutputFolder);
        File.ReadAllText(JsonPath).Should().Contain("\"outputFolder\": \"\"");
    }

    [Theory]
    [InlineData(@"E:\Old\Sic\userdata\Converted")]
    [InlineData(@"E:\Old\Sic\userdata\Converted\")]
    public void Load_LegacyPortableOutputFolderFromElsewhere_StaysWhenInstalled(string folder) {
        WriteLegacy($"OutputFolder={folder}");

        Load(isPortable: false).Config.General.OutputFolder.Should().Be(folder);
    }

    [Fact]
    public void Load_BothFiles_JsonWinsAndLegacyFileIsLeftAlone() {
        WriteJson(BuildJson());
        WriteLegacy("Language=fr", "OutputFolder=D:\\Legacy");

        var result = Load();

        result.Config.General.Should().BeEquivalentTo(ValidSettings());
        File.Exists(LegacyPath).Should().BeTrue();
        File.Exists(LegacyPath + ".bak").Should().BeFalse();
    }

    [Fact]
    public void Load_NoFiles_WritesDefaults() {
        var folder = Path.Combine(_folder, "new");

        var result = ConfigSerializer.Load(folder, isPortable: true);

        result.Config.General.Should().BeEquivalentTo(new GeneralSettings());
        ShouldBeSilent(result);
        File.Exists(Path.Combine(folder, App.ConfigFileName)).Should().BeTrue();

        // A file that wasn't complete JSON would take the corrupt-file path here.
        var reloaded = ConfigSerializer.Load(folder, isPortable: true);
        ShouldBeSilent(reloaded);
        reloaded.Config.General.Should().BeEquivalentTo(new GeneralSettings());
    }

    [Fact]
    public void Load_NoFilesWhenTheFolderCannotBeCreated_ReportsASaveErrorAndUsesDefaults() {
        var file = Path.Combine(_folder, "file.txt");
        File.WriteAllText(file, "");

        var result = ConfigSerializer.Load(Path.Combine(file, "data"), isPortable: true);

        result.SaveError.Should().StartWith("Unable to save configuration: ");
        result.LoadError.Should().BeNull();
        result.SavesBlocked.Should().BeFalse();
        result.Config.General.Should().BeEquivalentTo(new GeneralSettings());
    }

    [Fact]
    public void Load_LegacyFileWhenSicJsonCannotBeWritten_KeepsSicCfg() {
        WriteLegacy("Language=de", "ConfirmExitWithQueue=False");

        // File.Exists is false for a folder, so this takes the migration path, and no file can
        // replace it.
        Directory.CreateDirectory(JsonPath);

        var result = Load();

        result.SaveError.Should().StartWith("Unable to save configuration: ");
        result.LoadError.Should().BeNull();
        result.SavesBlocked.Should().BeFalse();
        result.Config.General.Language.Should().Be("de");
        result.Config.General.ConfirmExitWithQueue.Should().BeFalse();
        File.Exists(LegacyPath).Should().BeTrue();
        File.Exists(LegacyPath + ".bak").Should().BeFalse();
        TempFiles().Should().BeEmpty();
    }

    [Fact]
    public void Load_LegacyFileThatCannotBeRenamed_IsStillMigratedSilently() {
        WriteLegacy("Language=de", "ConfirmExitWithQueue=False");

        ConfigLoadResult result;

        // Reading still works, but the open handle stops the rename.
        using (new FileStream(LegacyPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            result = Load();
        }

        ShouldBeSilent(result);
        result.Config.General.Language.Should().Be("de");
        result.Config.General.ConfirmExitWithQueue.Should().BeFalse();
        File.Exists(JsonPath).Should().BeTrue();
        File.Exists(LegacyPath).Should().BeTrue();
        File.Exists(LegacyPath + ".bak").Should().BeFalse();

        File.WriteAllText(LegacyPath, "[General]\r\nLanguage=fr\r\n");
        var next = Load();
        ShouldBeSilent(next);
        next.Config.General.Should().BeEquivalentTo(result.Config.General);
    }

    // The next three call the step Load takes after finding no sic.json, with the files as another
    // process left them in the meantime.

    [Fact]
    public void CreateDefaults_SicJsonCreatedAfterTheCheck_IsKeptAndRead() {
        var json = BuildJson();
        WriteJson(json);

        var result = ConfigSerializer.CreateDefaults(JsonPath);

        result.Should().NotBeNull();
        ShouldBeSilent(result!);
        result!.Config.General.Should().BeEquivalentTo(ValidSettings());
        File.ReadAllText(JsonPath).Should().Be(json);
        TempFiles().Should().BeEmpty();
    }

    [Fact]
    public void MigrateLegacy_SicJsonCreatedAfterTheCheck_IsKeptAndRead() {
        var json = BuildJson();
        WriteJson(json);
        WriteLegacy("Language=fr", "OutputFolder=D:\\Legacy");

        var result = ConfigSerializer.MigrateLegacy(JsonPath, LegacyPath, isPortable: true);

        result.Should().NotBeNull();
        ShouldBeSilent(result!);
        result!.Config.General.Should().BeEquivalentTo(ValidSettings());
        File.ReadAllText(JsonPath).Should().Be(json);
        TempFiles().Should().BeEmpty();

        // Renaming it is the winner's job.
        File.Exists(LegacyPath).Should().BeTrue();
    }

    [Fact]
    public void MigrateLegacy_SicCfgRenamedAfterTheCheck_ReturnsNullAndWritesNothing() {
        ConfigSerializer.MigrateLegacy(JsonPath, LegacyPath, isPortable: true).Should().BeNull();

        File.Exists(JsonPath).Should().BeFalse();
    }

    #endregion

    #region Failure paths

    [Fact]
    public void Load_CorruptFile_IsBackedUpWithoutLossAndReplacedWithDefaults() {
        var bytes = Encoding.UTF8.GetBytes("{ \"general\": { \"language\": \"de\" ");
        File.WriteAllBytes(JsonPath, bytes);

        var result = Load();

        result.Config.General.Should().BeEquivalentTo(new GeneralSettings());
        result.LoadError.Should().Contain("sic.json.bad");
        result.SaveError.Should().BeNull();
        result.SavesBlocked.Should().BeFalse();
        File.ReadAllBytes(BadPath).Should().Equal(bytes);

        var next = Load();
        next.Config.General.Should().BeEquivalentTo(new GeneralSettings());
        ShouldBeSilent(next);
        Backups().Should().ContainSingle();
    }

    [Fact]
    public void Load_CorruptFileWhenTheBackupIsHeldOpen_KeepsTheFileAndBlocksSaves() {
        var bytes = Encoding.UTF8.GetBytes("not json");
        File.WriteAllBytes(JsonPath, bytes);
        File.WriteAllText(BadPath, "older backup");

        ConfigLoadResult result;

        using (new FileStream(BadPath, FileMode.Open, FileAccess.Read, FileShare.None)) {
            result = Load();
        }

        result.SavesBlocked.Should().BeTrue();
        result.LoadError.Should().Contain("won't be saved");
        File.ReadAllBytes(JsonPath).Should().Equal(bytes);
        File.ReadAllText(BadPath).Should().Be("older backup");
        Backups().Should().ContainSingle();
    }

    [Fact]
    public void Load_BadValueWhenTheBackupIsHeldOpen_KeepsTheFileAndBlocksSaves() {
        var json = BuildJson("updateCheckInterval", "\"Hourly\"");
        WriteJson(json);
        File.WriteAllText(BadPath, "older backup");

        ConfigLoadResult result;

        using (new FileStream(BadPath, FileMode.Open, FileAccess.Read, FileShare.None)) {
            result = Load();
        }

        result.SavesBlocked.Should().BeTrue();
        result.LoadError.Should().Contain("1 setting").And.Contain("won't be saved");
        result.Config.General.Should().BeEquivalentTo(ValidSettingsWithDefault("updateCheckInterval"));
        File.ReadAllText(JsonPath).Should().Be(json);
    }

    [Fact]
    public void Load_CorruptFileWithAnExistingBackup_RotatesTheOldBackup() {
        File.WriteAllText(BadPath, "older backup");
        WriteJson("{");

        Load();

        File.ReadAllText(BadPath).Should().Be("{");
        var rotated = Backups().Where(path => path != BadPath).Should().ContainSingle().Subject;
        RotatedBackupName().IsMatch(Path.GetFileName(rotated)).Should().BeTrue();
        File.ReadAllText(rotated).Should().Be("older backup");
    }

    [Fact]
    public void Load_LockedFile_BlocksSavesAndChangesNothing() {
        var json = BuildJson();
        WriteJson(json);

        ConfigLoadResult result;

        using (new FileStream(JsonPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
            result = Load();
        }

        result.SavesBlocked.Should().BeTrue();
        result.LoadError.Should().Contain("won't be saved");
        result.Config.General.Should().BeEquivalentTo(new GeneralSettings());
        File.ReadAllText(JsonPath).Should().Be(json);
        Backups().Should().BeEmpty();
    }

    [Fact]
    public void Load_LockedLegacyFile_KeepsItAndBlocksSaves() {
        WriteLegacy("Language=de");

        ConfigLoadResult result;

        using (new FileStream(LegacyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
            result = Load();
        }

        result.SavesBlocked.Should().BeTrue();
        result.LoadError.Should().Contain("won't be saved");
        result.Config.General.Should().BeEquivalentTo(new GeneralSettings());
        File.Exists(LegacyPath).Should().BeTrue();
        File.Exists(JsonPath).Should().BeFalse();
        File.Exists(LegacyPath + ".bak").Should().BeFalse();
    }

    #endregion

    #region BackUpFile

    [Fact]
    public void BackUpFile_NeverMovesTheFile() {
        WriteJson("{}");

        var backup = ConfigSerializer.BackUpFile(JsonPath);

        backup.Should().Be(BadPath);
        File.ReadAllText(JsonPath).Should().Be("{}");
        File.ReadAllText(BadPath).Should().Be("{}");
    }

    [Fact]
    public void BackUpFile_WhenTheTimestampedNameIsTaken_AddsANumber() {
        WriteJson("current");
        File.WriteAllText(BadPath, "older backup");

        // This second and the next, so the clock ticking over during the test changes nothing.
        var now = DateTime.Now;
        string[] taken = [
            $"{BadPath}.{now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}",
            $"{BadPath}.{now.AddSeconds(1).ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}",
        ];

        foreach (var path in taken) {
            File.WriteAllText(path, Path.GetFileName(path));
        }

        ConfigSerializer.BackUpFile(JsonPath);

        File.ReadAllText(BadPath).Should().Be("current");

        foreach (var path in taken) {
            File.ReadAllText(path).Should().Be(Path.GetFileName(path));
        }

        var numbered = Backups().Where(path => path.EndsWith("-1", StringComparison.Ordinal)).Should().ContainSingle().Subject;
        RotatedBackupName().IsMatch(Path.GetFileName(numbered)).Should().BeTrue();
        File.ReadAllText(numbered).Should().Be("older backup");
        Backups().Should().HaveCount(4);
    }

    #endregion

    #region Helpers

    private ConfigLoadResult Load(bool isPortable = true) => ConfigSerializer.Load(_folder, isPortable);

    private void WriteJson(string json) => File.WriteAllText(JsonPath, json, new UTF8Encoding(false));

    private void WriteLegacy(params string[] lines) =>
        File.WriteAllText(LegacyPath, "[General]\r\n" + string.Join("\r\n", lines) + "\r\n", new UTF8Encoding(false));

    private string[] Backups() => Directory.GetFiles(_folder, "sic.json.bad*");

    private string[] TempFiles() => Directory.GetFiles(_folder, "*.tmp");

    private static void ShouldBeSilent(ConfigLoadResult result) {
        result.LoadError.Should().BeNull();
        result.SaveError.Should().BeNull();
        result.SavesBlocked.Should().BeFalse();
    }

    /// <summary>A <c>sic.json</c> with <see cref="ValidValues"/>, one of them optionally replaced.</summary>
    private static string BuildJson(string? key = null, string? raw = null) {
        var values = ValidValues.Select(value => $"    \"{value.Key}\": {(value.Key == key ? raw : value.Raw)}");
        return "{\n  \"general\": {\n" + string.Join(",\n", values) + "\n  }\n}\n";
    }

    /// <summary>The settings <see cref="BuildJson"/> describes.</summary>
    private static GeneralSettings ValidSettings() => new() {
        Language = "de",
        OutputFolder = CustomFolder,
        SaveToSourceFolder = true,
        LastInputFolder = @"D:\In",
        ConfirmExitWithQueue = false,
        CheckForUpdatesOnStartup = false,
        DetectClipboardData = true,
        EnabledFormats = ["JPG", "PNG"],
        UpdateCheckInterval = UpdateCheckInterval.Weekly,
    };

    /// <summary><see cref="ValidSettings"/> with the setting <paramref name="key"/> names back at its default.</summary>
    private static GeneralSettings ValidSettingsWithDefault(string key) {
        var settings = ValidSettings();
        var defaults = new GeneralSettings();
        var property = typeof(GeneralSettings).GetProperties()
            .Single(candidate => string.Equals(candidate.Name, key, StringComparison.OrdinalIgnoreCase));
        property.SetValue(settings, property.GetValue(defaults));
        return settings;
    }

    #endregion
}
