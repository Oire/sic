using AwesomeAssertions;
using Oire.Sic.Services;
using Oire.Sic.Utils.Enums;
using Xunit;

namespace Oire.Sic.Tests;

public class UpdateIntervalTests {
    [Theory]
    [InlineData(UpdateCheckInterval.Daily, 1)]
    [InlineData(UpdateCheckInterval.EveryThreeDays, 3)]
    [InlineData(UpdateCheckInterval.Weekly, 7)]
    [InlineData(UpdateCheckInterval.Monthly, 30)]
    public void ToFrequency_MapsEachIntervalToItsPeriod(UpdateCheckInterval interval, int days) {
        UpdateService.ToFrequency(interval).Should().Be(TimeSpan.FromDays(days));
    }

    [Fact]
    public void ToFrequency_GivesNullForNever() {
        UpdateService.ToFrequency(UpdateCheckInterval.Never).Should().BeNull();
    }

    [Fact]
    public void ToFrequency_CoversEveryInterval() {
        foreach (var interval in Enum.GetValues<UpdateCheckInterval>()) {
            var frequency = UpdateService.ToFrequency(interval);

            if (interval == UpdateCheckInterval.Never) {
                frequency.Should().BeNull();
            } else {
                frequency.Should().NotBeNull($"{interval} must enable the background loop");
            }
        }
    }
}
