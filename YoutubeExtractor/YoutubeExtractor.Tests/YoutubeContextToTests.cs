using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoutubeExtractor.Interface;

namespace YoutubeExtractor.Tests {

    [TestClass]
    public class YoutubeContextToTests {
        //const string Url = "https://www.youtube.com/watch?v=8SPtkjMUkGk";
        const string Url = "https://www.youtube.com/watch?v=uxpDa-c-4Mc";
        
        [TestMethod]
        public void ContextToAudio() {
            var yc = new YoutubeContext(Url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
            try {
                yc.ToAudio();
                Assert.IsTrue(File.Exists(yc.AudioPath.FullName));
                Debug.WriteLine(yc.AudioPath.FullName);
            } finally {
                if (yc.AudioPath !=null && File.Exists(yc.AudioPath.FullName))
                    File.Delete(yc.AudioPath.FullName);
                Assert.IsFalse(File.Exists(yc.AudioPath.FullName));
            }
        }

        [TestMethod]
        public void ContextToAudioAsync() {
            var yc = new YoutubeContext(Url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
            try {
                yc.ToAudioAsync().Wait();
                Assert.IsTrue(File.Exists(yc.AudioPath.FullName));
                Debug.WriteLine(yc.AudioPath.FullName);
            } finally {
                if (yc.AudioPath !=null && File.Exists(yc.AudioPath.FullName))
                    File.Delete(yc.AudioPath.FullName);
                Assert.IsFalse(File.Exists(yc.AudioPath.FullName));
            }
        }

        [TestMethod]
        public void ContextToVideo() {
            var yc = new YoutubeContext(Url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
            try {
                yc.ToVideo(VideoType.Mp4);
                Assert.IsTrue(File.Exists(yc.VideoPath.FullName));
                Debug.WriteLine(yc.VideoPath.FullName);
            } finally {
                if (yc.VideoPath != null && File.Exists(yc.VideoPath.FullName))
                    File.Delete(yc.VideoPath.FullName);
                Assert.IsFalse(File.Exists(yc.VideoPath.FullName));
            }
        }

        
        [TestMethod]
        public void ContextToVideoAsync() {
            var yc = new YoutubeContext(Url) {BaseDirectory = new DirectoryInfo(Path.GetTempPath())};
            try {
                yc.ToVideoAsync(VideoType.Mp4).Wait();
                Assert.IsTrue(File.Exists(yc.VideoPath.FullName));
                Debug.WriteLine(yc.VideoPath.FullName);
            } finally {
                if (yc.VideoPath != null && File.Exists(yc.VideoPath.FullName))
                    File.Delete(yc.VideoPath.FullName);
                Assert.IsFalse(File.Exists(yc.VideoPath.FullName));
            }
        }

    }
}