using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoutubeExtractor.Interface;

namespace YoutubeExtractor.Tests {

    [TestClass]
    public class YoutubeUrlParserTests {
        const string url = "https://www.youtube.com/watch?v=6WJFjXtHcy4";
        [TestMethod]
        public void NotYoutube() {
            Assert.IsNull(DownloadUrlResolver.NormalizeYoutubeUrl("https://www.facebook.com/"));
            Assert.IsNull(DownloadUrlResolver.NormalizeYoutubeUrl("https://www.facebook.com/?q=youtube"));
            Assert.IsNull(DownloadUrlResolver.NormalizeYoutubeUrl("https://www.google.com/?q=youtube.com"));
            Assert.IsNull(DownloadUrlResolver.NormalizeYoutubeUrl("youtube.com/"));
        }

        [TestMethod]
        public void RegularYoutubeUrl() {
            Assert.AreEqual(url, DownloadUrlResolver.NormalizeYoutubeUrl(url));
        }

        [TestMethod]
        public void ShortYoutubeUrl() {
            Assert.AreEqual(url, DownloadUrlResolver.NormalizeYoutubeUrl("https://youtu.be/6WJFjXtHcy4"));

            //https://youtu.be/6WJFjXtHcy4
        }

        [TestMethod]
        public void PlaylistUrl() {
            Assert.AreEqual(url, DownloadUrlResolver.NormalizeYoutubeUrl("https://www.youtube.com/watch?v=6WJFjXtHcy4&list=RD6WJFjXtHcy4#t=10"));

            //https://www.youtube.com/watch?v=6WJFjXtHcy4&list=RD6WJFjXtHcy4#t=10
        }

    }
}