using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoutubeExtractor.Interface;

namespace YoutubeExtractor.Tests {

    [TestClass]
    public class EventOrderTest {
        private const string Url = "https://www.youtube.com/watch?v=8SPtkjMUkGk";

        [TestMethod]
        public void UrlDownloadingTest() {
            var yc = new YoutubeContext(Url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
            DownloadUrlResolver.FindHighestAudioQualityDownloadUrl(yc);

            var ad = new AudioDownloader(yc);
            try {
                ad.Execute();
                Assert.IsTrue(File.Exists(yc.AudioPath.FullName));
                Debug.WriteLine(yc.AudioPath.FullName);
            } finally {
                if (yc != null && File.Exists(yc.AudioPath.FullName))
                    File.Delete(yc.AudioPath.FullName);
            }
        }

        [TestMethod]
        public void UrlDownloadingFileAlreadyExistsTest() {
            var yc = new YoutubeContext(Url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
            var yc2 = new YoutubeContext(Url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
            DownloadUrlResolver.FindHighestAudioQualityDownloadUrlAsync(yc).Wait();
            DownloadUrlResolver.FindHighestAudioQualityDownloadUrlAsync(yc2).Wait();

            if (yc.VideoInfo.RequiresDecryption)
                DownloadUrlResolver.DecryptDownloadUrl(yc.VideoInfo);
            var tf = Path.GetTempPath();
            var ad = new AudioDownloader(yc);
            var ad2 = new AudioDownloader(yc2);
            try {
                ad.Execute();
                ad2.Execute();
                Debug.WriteLine(yc.AudioPath.FullName);
                Debug.WriteLine(yc2.AudioPath.FullName);
                Assert.IsTrue(File.Exists(yc.AudioPath.FullName));
                Assert.IsTrue(File.Exists(yc2.AudioPath.FullName));
                Assert.IsTrue(yc.AudioPath.FullName != yc2.AudioPath.FullName);
            } finally {
                if (ad != null && File.Exists(yc.AudioPath.FullName))
                    File.Delete(yc.AudioPath.FullName);
                if (ad2 != null && File.Exists(yc2.AudioPath.FullName))
                    File.Delete(yc2.AudioPath.FullName);
            }
        }

        [TestMethod]
        public void GetHighestAudioQualiyBothAsyncTest() {
            var yc = new YoutubeContext(Url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
            DownloadUrlResolver.FindHighestAudioQualityDownloadUrlAsync(yc).Wait();
            var highest2task = DownloadUrlResolver.GetHighestAudioQualityDownloadUrlAsync(Url);
            var highest = DownloadUrlResolver.GetHighestAudioQualityDownloadUrl(Url);
            var highest2 = highest2task.Result;
            Assert.AreEqual(yc.VideoInfo, highest);
            Assert.AreEqual(yc.VideoInfo, highest2);
            Assert.AreEqual(highest, highest2);
            Debug.WriteLine(yc.VideoInfo);
        }

        [TestMethod]
        public void GetHighestVideoQualiyBothAsyncTest() {
            var yc = new YoutubeContext("https://www.youtube.com/watch?v=WlozEc6BxsE") {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
            DownloadUrlResolver.FindHighestVideoQualityDownloadUrlAsync(yc).Wait();
            var highest2task = DownloadUrlResolver.GetHighestVideoQualityDownloadUrlAsync("https://www.youtube.com/watch?v=WlozEc6BxsE");
            var highest = DownloadUrlResolver.GetHighestVideoQualityDownloadUrl("https://www.youtube.com/watch?v=WlozEc6BxsE");
            var highest2 = highest2task.Result;
            Assert.AreEqual(yc.VideoInfo, highest);
            Assert.AreEqual(yc.VideoInfo, highest2);
            Assert.AreEqual(highest, highest2);
            Debug.WriteLine(yc.VideoInfo);
        }

        [TestMethod]
        public void EventListeningTest() {
            var yc = new YoutubeContext(Url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
            var ad = new AudioDownloader(yc);

            var sb = new StringBuilder();

            yc.DownloadFailed += (sender, args) => {
                Debug.WriteLine(args.Subject + "\n" + args.Exception);
                sb.AppendLine(args.Subject + "\n" + args.Exception);
            };

            yc.ProgresStateChanged += (sender, args) => {
                if (args.UIHandled || args.Stage != YoutubeStage.ExtractingAudio)
                    return;
                Debug.Write(args.Precentage.ToString("A###") + " ");
                sb.Append(args.Precentage.ToString("A###") + " ");
                args.UIHandled = true;
            };
            yc.ProgresStateChanged += (sender, args) => {
                if (args.UIHandled || args.Stage != YoutubeStage.Downloading)
                    return;
                Debug.Write(args.Precentage.ToString("D###") + " ");
                sb.Append(args.Precentage.ToString("D###") + " ");
                args.UIHandled = true;
            };

            yc.ProgresStateChanged += (sender, args) => {
                if (args.UIHandled)
                    return;
                Debug.Write($"{{{args.Stage.ToString()}}}");
                sb.Append($"{{{args.Stage.ToString()}}}");
            };

            DownloadUrlResolver.FindHighestAudioQualityDownloadUrl(yc);

            try {
                ad.Execute();
                Assert.IsTrue(File.Exists(yc.AudioPath?.FullName ?? "/"));
                Debug.WriteLine(yc.AudioPath?.FullName ?? "");
            } finally {
                if (yc.AudioPath != null && File.Exists(yc.AudioPath.FullName))
                    File.Delete(yc.AudioPath.FullName);
            }

            var events = Enum.GetValues(typeof(YoutubeStage)).Cast<YoutubeStage>()
                .Where(s =>
                    s != YoutubeStage.DecipheringUrls
                    && s != YoutubeStage.Downloading
                    && s != YoutubeStage.ExtractingAudio
                    && s != YoutubeStage.Undefined
                )
                .Select(ys => $"{{{ys}}}").ToArray();
            var c = sb.ToString();
            foreach (var @event in events)
                Assert.IsTrue(c.Contains(@event), $"c.Contains(YoutubeStage.{@event})");
        }
    }
}