using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Oire.Sic.Tests;

/// <summary>One translated entry of a <c>Sic.po</c> catalog.</summary>
/// <param name="MsgId">The English source text.</param>
/// <param name="MsgIdPlural">The English plural, or null for a singular entry.</param>
/// <param name="MsgStrs">The translations: one for a singular entry, one per plural form otherwise.</param>
/// <param name="IsFuzzy">Whether the entry carries a <c>#, fuzzy</c> flag.</param>
/// <param name="Line">The line its <c>msgid</c> is on, for failure messages.</param>
internal sealed record PoEntry(string MsgId, string? MsgIdPlural, IReadOnlyList<string> MsgStrs, bool IsFuzzy,
    int Line) {
    public bool IsPlural => MsgIdPlural is not null;

    public override string ToString() => $"line {Line}: \"{MsgId}\"";
}

/// <summary>
/// A small reader for SIC!'s gettext catalogs, enough to check them: multi-line strings and
/// plural entries are handled, the header is read for <c>nplurals</c> and then skipped, and
/// obsolete <c>#~</c> entries are skipped. A flag such as <c>#, fuzzy</c> belongs to the entry
/// after it, which is how every flag in today's catalogs sits on an obsolete entry.
/// </summary>
internal sealed partial class PoCatalog {
    private static readonly Lazy<string> RepositoryRootValue = new(FindRepositoryRoot);

    private PoCatalog(string language, string path, int pluralFormCount, IReadOnlyList<PoEntry> entries) {
        Language = language;
        FilePath = path;
        PluralFormCount = pluralFormCount;
        Entries = entries;
    }

    /// <summary>The folder holding <c>Sic.sln</c>, found by walking up from the test assembly.</summary>
    public static string RepositoryRoot => RepositoryRootValue.Value;

    /// <summary>The folder holding one subfolder per language.</summary>
    public static string LocaleFolder => Path.Combine(RepositoryRoot, "src", "Sic", "locale");

    /// <summary>Every language with a <c>Sic.po</c> on disk, in ordinal order.</summary>
    public static IReadOnlyList<string> Languages =>
        [.. Directory.EnumerateDirectories(LocaleFolder)
            .Where(folder => File.Exists(Path.Combine(folder, "Sic.po")))
            .Select(folder => Path.GetFileName(folder))
            .Order(StringComparer.Ordinal)];

    public string Language { get; }

    public string FilePath { get; }

    /// <summary>The <c>nplurals</c> value from the header's <c>Plural-Forms</c>.</summary>
    public int PluralFormCount { get; }

    /// <summary>Every live entry, without the header and without obsolete entries.</summary>
    public IReadOnlyList<PoEntry> Entries { get; }

    public static PoCatalog Load(string language) {
        var path = Path.Combine(LocaleFolder, language, "Sic.po");
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        var entries = new List<PoEntry>();
        string? header = null;
        var builder = new EntryBuilder();

        void Flush() {
            if (builder.IsEmpty) {
                return;
            }

            if (!builder.IsObsolete) {
                if (builder.MsgId.Length == 0) {
                    header = builder.MsgStrs.GetValueOrDefault(0, string.Empty);
                } else {
                    entries.Add(builder.Build());
                }
            }

            builder = new EntryBuilder();
        }

        for (var index = 0; index < lines.Length; index++) {
            var line = lines[index].Trim();

            if (line.Length == 0) {
                Flush();
                continue;
            }

            if (line.StartsWith('#')) {
                // A comment after a translation starts the next entry, even with no blank line between.
                if (builder.HasTranslation) {
                    Flush();
                }

                if (line.StartsWith("#~", StringComparison.Ordinal)) {
                    builder.IsObsolete = true;
                } else if (line.StartsWith("#,", StringComparison.Ordinal)) {
                    builder.IsFuzzy |= line[2..].Split(',').Any(flag => flag.Trim() == "fuzzy");
                }

                builder.Touch();
                continue;
            }

            if (line.StartsWith('"')) {
                builder.Append(Unquote(line, path, index));
                continue;
            }

            var keyword = KeywordPattern().Match(line);
            if (!keyword.Success) {
                throw new FormatException($"{path}:{index + 1}: cannot read \"{line}\".");
            }

            var name = keyword.Groups["name"].Value;
            if (name == "msgid" && builder.HasTranslation) {
                Flush();
            }

            var form = keyword.Groups["form"].Success
                ? int.Parse(keyword.Groups["form"].Value, CultureInfo.InvariantCulture)
                : (int?)null;
            builder.Start(name, form, index + 1);
            builder.Append(Unquote(line[keyword.Length..].Trim(), path, index));
        }

        Flush();

        var plurals = header is null ? null : NPluralsPattern().Match(header);
        var pluralFormCount = plurals is { Success: true }
            ? int.Parse(plurals.Groups[1].Value, CultureInfo.InvariantCulture)
            : 2;

        return new PoCatalog(language, path, pluralFormCount, entries);
    }

    /// <summary>The entry for <paramref name="msgId"/>, or null when the catalog lacks it.</summary>
    public PoEntry? Find(string msgId) => Entries.FirstOrDefault(entry => entry.MsgId == msgId);

    private static string Unquote(string text, string path, int index) {
        if (text.Length < 2 || text[0] != '"' || text[^1] != '"') {
            throw new FormatException($"{path}:{index + 1}: expected a quoted string, got \"{text}\".");
        }

        var result = new StringBuilder(text.Length);
        for (var position = 1; position < text.Length - 1; position++) {
            var character = text[position];
            if (character != '\\' || position == text.Length - 2) {
                result.Append(character);
                continue;
            }

            position++;
            result.Append(text[position] switch {
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                var other => other,
            });
        }

        return result.ToString();
    }

    private static string FindRepositoryRoot() {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent) {
            if (File.Exists(Path.Combine(folder.FullName, "Sic.sln"))) {
                return folder.FullName;
            }
        }

        throw new DirectoryNotFoundException($"No Sic.sln above {AppContext.BaseDirectory}.");
    }

    [GeneratedRegex(@"^(?<name>msgctxt|msgid_plural|msgid|msgstr)(?:\[(?<form>\d+)\])?")]
    private static partial Regex KeywordPattern();

    [GeneratedRegex(@"nplurals\s*=\s*(\d+)")]
    private static partial Regex NPluralsPattern();

    /// <summary>Collects one entry's keywords and strings while the reader walks its lines.</summary>
    private sealed class EntryBuilder {
        private readonly StringBuilder _msgId = new();
        private readonly StringBuilder _msgIdPlural = new();
        private readonly SortedDictionary<int, StringBuilder> _msgStrs = [];
        private StringBuilder? _current;
        private bool _hasPlural;
        private int _line;

        public bool IsEmpty { get; private set; } = true;

        public bool IsObsolete { get; set; }

        public bool IsFuzzy { get; set; }

        public bool HasTranslation => _msgStrs.Count > 0;

        public string MsgId => _msgId.ToString();

        public Dictionary<int, string> MsgStrs => _msgStrs.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());

        public void Touch() => IsEmpty = false;

        public void Start(string keyword, int? form, int line) {
            IsEmpty = false;
            switch (keyword) {
                case "msgid":
                    _line = line;
                    _current = _msgId;
                    break;
                case "msgid_plural":
                    _hasPlural = true;
                    _current = _msgIdPlural;
                    break;
                case "msgstr":
                    _current = new StringBuilder();
                    _msgStrs[form ?? 0] = _current;
                    break;
                default:
                    // msgctxt: SIC! uses none today, and its text is not checked.
                    _current = new StringBuilder();
                    break;
            }
        }

        public void Append(string text) => _current?.Append(text);

        public PoEntry Build() => new(
            MsgId,
            _hasPlural ? _msgIdPlural.ToString() : null,
            [.. _msgStrs.Values.Select(text => text.ToString())],
            IsFuzzy,
            _line);
    }
}
