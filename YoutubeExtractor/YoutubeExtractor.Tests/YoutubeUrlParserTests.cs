using System.Diagnostics;
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

        [TestMethod]
        public void PlaylistNotYoutube() {
            Assert.IsNull(DownloadUrlResolver.NormalizeYoutubePlaylistUrl("https://www.facebook.com/"));
            Assert.IsNull(DownloadUrlResolver.NormalizeYoutubePlaylistUrl("https://www.facebook.com/?q=youtube"));
            Assert.IsNull(DownloadUrlResolver.NormalizeYoutubePlaylistUrl("https://www.google.com/?q=youtube.com"));
            Assert.IsNull(DownloadUrlResolver.NormalizeYoutubePlaylistUrl("youtube.com/"));
        }

        [TestMethod]
        public void PlaylistRegularYoutubeUrl() {
            Assert.IsNull(DownloadUrlResolver.NormalizeYoutubePlaylistUrl(url));
        }

        [TestMethod]
        public void PlaylistShortYoutubeUrl() {
            Assert.IsNull(DownloadUrlResolver.NormalizeYoutubePlaylistUrl("https://youtu.be/6WJFjXtHcy4"));
        }

        [TestMethod]
        public void BasicPlaylistUrl() {
            var res = DownloadUrlResolver.NormalizeYoutubePlaylistUrl("https://www.youtube.com/watch?v=6WJFjXtHcy4&list=RD6WJFjXtHcy4#t=10");
            Debug.WriteLine(res);
            Assert.IsNotNull(res);
        }

        [TestMethod]
        public void BasicPlaylistUrlWithJunkParameters() {
            var res = DownloadUrlResolver.NormalizeYoutubePlaylistUrl("https://www.youtube.com/watch?v=6WJFjXtHcy4&app=desktop&list=RD6WJFjXtHcy4&anal=isfun#t=10");
            Debug.WriteLine(res);
            Assert.IsNotNull(res);
        }
        [TestMethod]
        public void PlaylistWithHashInIt() {
            var url = "https://www.youtube.com/watch?v=6WJFjXtHcy4&list=RD6WJFjXtHcy4#t=10";
            Assert.AreEqual("https://www.youtube.com/watch?v=6WJFjXtHcy4&list=RD6WJFjXtHcy4", 
                DownloadUrlResolver.NormalizeYoutubePlaylistUrl(url));
        }

        [TestMethod]
        public void PlaylistPageUrl() {
            var url = "https://www.youtube.com/playlist?list=PLFgquLnL59am-L9YIrwrGCc7L-10Ff3vZ";
            var res = DownloadUrlResolver.NormalizeYoutubePlaylistUrl(url);
            Debug.WriteLine(url);
            Debug.WriteLine(res);
            Assert.IsNotNull(res);
        }

        [TestMethod]
        public void PlaylistShareUrlWithJunkParameters() {
            var url = "https://www.youtube.com/playlist?anal=isfun&list=PLFgquLnL59am-L9YIrwrGCc7L-10Ff3vZ&app=desktop";
            var res = DownloadUrlResolver.NormalizeYoutubePlaylistUrl(url);
            Debug.WriteLine(url);
            Debug.WriteLine(res);
            Assert.IsNotNull(res);

        }

        [TestMethod]
        public void RegularVideoWithPlaylistShareUrl() {
            var url = "https://youtu.be/6ACl8s_tBzE?list=RDebXbLfLACGM";
            var res = DownloadUrlResolver.NormalizeYoutubePlaylistUrl(url);
            Debug.WriteLine(url);
            Debug.WriteLine(res);
            Assert.IsNotNull(res);
        }

        [TestMethod]
        public void RegularVideoWithPlaylistShareUrlAsAYoutubeUrl() {
            var url = "https://youtu.be/6ACl8s_tBzE?list=RDebXbLfLACGM";
            var res = DownloadUrlResolver.NormalizeYoutubeUrl(url);
            Debug.WriteLine(url);
            Debug.WriteLine(res);
            Assert.IsNotNull(res);
        }

        [TestMethod]
        public void IsPlaylistTest() {
            Assert.IsTrue(DownloadUrlResolver.IsPlaylistUrl("https://youtu.be/6ACl8s_tBzE?list=RDebXbLfLACGM"));
            Assert.IsTrue(DownloadUrlResolver.IsPlaylistUrl("https://www.youtube.com/playlist?anal=isfun&list=PLFgquLnL59am-L9YIrwrGCc7L-10Ff3vZ&app=desktop"));
            Assert.IsTrue(DownloadUrlResolver.IsPlaylistUrl("https://www.youtube.com/watch?v=6WJFjXtHcy4&list=RD6WJFjXtHcy4"));
            Assert.IsTrue(DownloadUrlResolver.IsPlaylistUrl("https://www.youtube.com/watch?v=6WJFjXtHcy4&app=desktop&list=RD6WJFjXtHcy4&anal=isfun#t=10"));
            Assert.IsFalse(DownloadUrlResolver.IsPlaylistUrl("https://youtu.be/6WJFjXtHcy4"));
        }
    }
}