using AwesomeAssertions;
using Oire.Sic.Utils;
using Xunit;

namespace Oire.Sic.Tests;

public class UrlHelperTests {
    [Theory]
    [InlineData("http://example.com/image.png", "http://example.com/image.png")]
    [InlineData("https://example.com/image.png", "https://example.com/image.png")]
    [InlineData("HTTPS://EXAMPLE.COM/photo.jpg", "HTTPS://EXAMPLE.COM/photo.jpg")]
    [InlineData("  https://example.com/a.webp  ", "https://example.com/a.webp")]
    [InlineData("\thttp://example.com/a.gif\r\n", "http://example.com/a.gif")]
    public void IsValidHttpUrl_AcceptsHttpAndHttpsLinks_AndReturnsThemTrimmed(string text, string expected) {
        UrlHelper.IsValidHttpUrl(text, out var url).Should().BeTrue();
        url.Should().Be(expected);
    }

    [Theory]
    [InlineData("ftp://example.com/image.png")]
    [InlineData("file:///C:/images/image.png")]
    [InlineData("mailto:someone@example.com")]
    [InlineData("data:image/png;base64,iVBORw0KGgo=")]
    [InlineData("/images/image.png")]
    [InlineData("images/image.png")]
    [InlineData("example.com/image.png")]
    [InlineData("not a link")]
    public void IsValidHttpUrl_RejectsOtherSchemesAndRelativeLinks(string text) {
        UrlHelper.IsValidHttpUrl(text, out var url).Should().BeFalse();
        url.Should().Be(text.Trim());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidHttpUrl_RejectsEmptyInput_AndReturnsAnEmptyString(string? text) {
        UrlHelper.IsValidHttpUrl(text, out var url).Should().BeFalse();
        url.Should().BeEmpty();
    }
}
