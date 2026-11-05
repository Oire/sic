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

    [Theory]
    [InlineData("http://example.com/images/photo.png", "photo.png")]
    [InlineData("http://example.com/my%20photo.png", "my photo.png")]
    [InlineData("http://example.com/photo.png?size=large#top", "photo.png")]
    [InlineData("http://example.com/C%3Aphoto.png", "C_photo.png")]
    [InlineData("http://example.com/File:Photo.png", "File_Photo.png")]
    [InlineData("http://example.com/a%5Cb.png", "a_b.png")]
    [InlineData("http://example.com/a%2Fb.png", "a_b.png")]
    [InlineData("http://example.com/a%3Fb.png", "a_b.png")]
    [InlineData("http://example.com/%2A%22%3C%3E%7C.png", "_____.png")]
    [InlineData("http://example.com/photo.png.%20.", "photo.png")]
    public void GetDownloadFileName_UnescapesAndCleansTheLastSegment(string link, string expected) {
        UrlHelper.GetDownloadFileName(new Uri(link)).Should().Be(expected);
    }

    [Theory]
    [InlineData("http://example.com")]
    [InlineData("http://example.com/")]
    [InlineData("http://example.com/images/")]
    [InlineData("http://example.com/%20%20")]
    public void GetDownloadFileName_FallsBackForALinkWithoutAFileName(string link) {
        UrlHelper.GetDownloadFileName(new Uri(link)).Should().Be(UrlHelper.DefaultDownloadName);
    }
}
