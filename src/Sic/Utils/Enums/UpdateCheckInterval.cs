namespace Oire.Sic.Utils.Enums;

/// <summary>
/// How often the app checks for updates in the background while running. Independent of the
/// one-shot "check on startup" preference. <see cref="Never"/> disables the background loop
/// entirely. Declared most-frequent-first so the default (<see cref="Daily"/>) is the zero
/// value and the enum order matches the Settings combo box order.
/// </summary>
public enum UpdateCheckInterval {
    /// <summary>Every 24 hours.</summary>
    Daily,

    /// <summary>Every 3 days.</summary>
    EveryThreeDays,

    /// <summary>Every 7 days.</summary>
    Weekly,

    /// <summary>Every 30 days.</summary>
    Monthly,

    /// <summary>Background checks disabled.</summary>
    Never,
}
