using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YoutubeExtractor.Tests {
    /// <summary>
    ///     Small series of unit tests for DownloadUrlResolver. Run these with NUnit.
    /// </summary>
    [TestClass]
    public class DownloadUrlResolverTest {
        [TestMethod]
        public void TryNormalizedUrlForStandardYouTubeUrlShouldReturnSame() {
            var url = "https://www.youtube.com/watch?v=12345";

            var normalizedUrl = string.Empty;

            Assert.IsTrue(DownloadUrlResolver.TryNormalizeYoutubeUrl(url, out normalizedUrl));
            Assert.AreEqual(url, normalizedUrl);
        }

        [TestMethod]
        public void TryNormalizedUrlForFullScreenUrlShouldReturnSame() {
            var url = "https://www.youtube.com/v/3X76Z9sl5Zg";

            var normalizedUrl = string.Empty;

            Assert.IsTrue(DownloadUrlResolver.TryNormalizeYoutubeUrl(url, out normalizedUrl));
            Assert.AreEqual("https://www.youtube.com/watch?v=3X76Z9sl5Zg", normalizedUrl);
        }

        [TestMethod]
        public void TryNormalizedrlForYouTuDotBeUrlShouldReturnNormalizedUrl() {
            var url = "http://youtu.be/12345";

            var normalizedUrl = string.Empty;
            Assert.IsTrue(DownloadUrlResolver.TryNormalizeYoutubeUrl(url, out normalizedUrl));
            Assert.AreEqual("https://www.youtube.com/watch?v=12345", normalizedUrl);
        }

        [TestMethod]
        public void TryNormalizedUrlForMobileLinkShouldReturnNormalizedUrl() {
            var url = "http://m.youtube.com/?v=12345";

            var normalizedUrl = string.Empty;
            Assert.IsTrue(DownloadUrlResolver.TryNormalizeYoutubeUrl(url, out normalizedUrl));

            Assert.AreEqual("https://www.youtube.com/watch?v=12345", normalizedUrl);
        }

        [TestMethod]
        public void GetNormalizedYouTubeUrlForBadLinkShouldReturnNull() {
            var url = "http://notAYouTubeUrl.com";

            var normalizedUrl = string.Empty;
            Assert.IsFalse(DownloadUrlResolver.TryNormalizeYoutubeUrl(url, out normalizedUrl));
            Assert.IsNull(normalizedUrl);
        }
    }
}