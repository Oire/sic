using GetText.WindowsForms;
using Oire.Sic.Models;
using Oire.Sic.Utils;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

public partial class FitToSizeResultsDialog: Form {
    private readonly List<SizeFitProposal> _proposals;

    /// <summary>The proposal the user chose, valid only after the dialog returns <see cref="DialogResult.OK"/>.</summary>
    public SizeFitProposal? SelectedProposal {
        get {
            var index = resultsListBox.SelectedIndex;
            return index >= 0 && index < _proposals.Count ? _proposals[index] : null;
        }
    }

    public FitToSizeResultsDialog(IReadOnlyList<SizeFitProposal> proposals) {
        _proposals = [.. proposals];

        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog);
        Text = _("Fit to File Size — Results");

        foreach (var proposal in _proposals) {
            resultsListBox.Items.Add(Describe(proposal));
        }

        if (resultsListBox.Items.Count > 0) {
            // The proposals arrive ranked best-first, so the recommended one sits at the top.
            resultsListBox.SelectedIndex = 0;
        }
    }

    private static string Describe(SizeFitProposal proposal) {
        var dimensions = _("{0}x{1}", proposal.Width, proposal.Height);
        var size = FormatSize(proposal.FileSize);

        var description = proposal.Quality.HasValue
            ? _("{0} — {1}, quality {2}% — {3}", proposal.Format, dimensions, proposal.Quality.Value, size)
            : _("{0} — {1}, full quality — {2}", proposal.Format, dimensions, size);

        return proposal.Recommended ? _("{0} (recommended)", description) : description;
    }

    private static string FormatSize(long bytes) {
        return bytes switch {
            < 1024 => _("{0} B", bytes),
            < 1024 * 1024 => _("{0} KB", (bytes / 1024.0).ToString("F1")),
            _ => _("{0} MB", (bytes / (1024.0 * 1024.0)).ToString("F1")),
        };
    }
}
