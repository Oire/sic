using Xunit;

namespace Oire.Sic.Tests;

/// <summary>
/// The collection for SIC!'s static state: tests that touch <c>Config</c>, <c>Console</c> or
/// <c>Localization</c>, or assert on the text <c>_()</c> returns. They run in this collection,
/// one at a time and apart from every other test, because <c>Localization.SetLanguage</c>
/// changes the culture of the whole process.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class StaticStateCollection {
    public const string Name = "StaticState";
}
