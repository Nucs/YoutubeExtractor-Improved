using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace YoutubeExtractor.Tests {
    /// <summary>
    ///     Small series of unit tests for DownloadUrlResolver. Run these with NUnit.
    /// </summary>
    [TestClass]
    public class YoutubeThumbnailTests {

        public static string nosd = "https://www.youtube.com/watch?v=8SPtkjMUkGk";
        public static string uptosd = "https://www.youtube.com/watch?v=t7Xv0P4LviE";

        [TestMethod]
        public void HighestThumbnailQualityFinderOnUPTOSDTest() {
            var n = new YoutubeThumbnail(uptosd);
            Assert.AreEqual("https://i.ytimg.com/vi/t7Xv0P4LviE/sddefault.jpg", n.Thumbnail);
        }
        [TestMethod]
        public void HighestThumbnailQualityFinderOnUPTOSDWithYoutubeContextTest() {
            var n = new YoutubeThumbnail(new YoutubeContext(uptosd));
            Assert.AreEqual("https://i.ytimg.com/vi/t7Xv0P4LviE/sddefault.jpg", n.Thumbnail);
        }

        [TestMethod]
        public void HighestThumbnailQualityFinderOnNOSDTest() {
            var n = new YoutubeThumbnail(nosd);
            Assert.AreNotEqual("https://i.ytimg.com/vi/8SPtkjMUkGk/sddefault.jpg", n.Thumbnail);
            Assert.AreEqual("https://i.ytimg.com/vi/8SPtkjMUkGk/hqdefault.jpg", n.Thumbnail);
        }
        
        [TestMethod]
        public void ThumbnailReturnsDefaultWhenNotFinishedYetOnNOSDTest() {
            var n = new YoutubeThumbnail(nosd);
            Assert.AreEqual("Resources/default.png", n.SafeThumbnail);
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public void ContextWithThumbnailUPTOSDTest() {
            var token = new CancellationTokenSource();
            var n = new YoutubeContext(uptosd, true);
            n.ProgresStateChanged += (sender, args) => {
                if (args.Stage == YoutubeStage.ThumbnailFound) {
                    token.Cancel();
                }
            };

            Task.Delay(15000, token.Token).Wait(token.Token);
        }

        [TestMethod]
        public void ContextWithoutThumbnailUPTOSDTest() {
            var token = new CancellationTokenSource();
            var n = new YoutubeContext(uptosd, false);
            n.ProgresStateChanged += (sender, args) => {
                if (args.Stage == YoutubeStage.ThumbnailFound)
                    token.Cancel();
            };

            Task.Delay(5000, token.Token).Wait(token.Token);
            Assert.IsTrue(n.Thumbnail == "Resources/default.png");
        }
    }
}