namespace Oire.Sic.Models;

/// <summary>
/// One viable way to bring an image under a requested file-size budget (issue #24). Produced by
/// <c>ImageConverter.FindSizeFitProposals</c>: a concrete target format, output dimensions (always
/// proportional to the source), and — for lossy formats — the encoding quality that was used.
/// <c>ImageConverter.ConvertToProposal</c> reproduces exactly this result on disk.
/// </summary>
public sealed record SizeFitProposal {
    /// <summary>The SIC! format key (e.g. <c>"JPG"</c>, <c>"WEBP"</c>).</summary>
    public required string Format { get; init; }

    /// <summary>Output width in pixels.</summary>
    public required int Width { get; init; }

    /// <summary>Output height in pixels.</summary>
    public required int Height { get; init; }

    /// <summary>Encoding quality (1–100) for lossy formats, or <c>null</c> for lossless formats
    /// where quality is not a lever.</summary>
    public int? Quality { get; init; }

    /// <summary>The size in bytes the encoded result will occupy — always at or under the budget.</summary>
    public required long FileSize { get; init; }

    /// <summary><c>true</c> when the output is smaller than the source in either dimension.</summary>
    public required bool Resized { get; init; }

    /// <summary><c>true</c> for the single proposal SIC! suggests first (largest result with no
    /// quality loss). Set by the ranking step, not part of the value identity.</summary>
    public bool Recommended { get; set; }
}
