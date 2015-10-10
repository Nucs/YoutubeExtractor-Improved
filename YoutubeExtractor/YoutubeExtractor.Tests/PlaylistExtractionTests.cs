using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YoutubeExtractor.Interface;

namespace YoutubeExtractor.Tests {

    [TestClass]
    public class PlaylistExtractionTests {
        const string SideSongPlaying = "https://www.youtube.com/watch?v=ebXbLfLACGM&index=11&list=RDO-zpOMYRi0w";
        const string PlaylistPage = "https://www.youtube.com/playlist?list=PLFgquLnL59am-L9YIrwrGCc7L-10Ff3vZ";

        [TestMethod]
        public void BasicPlayingSongSidePlaylistExtractionTest() {
            var urls = DownloadUrlResolver.ExtractPlaylist(SideSongPlaying);
            Assert.IsNotNull(urls);
            Assert.IsTrue(urls.Count>0);
            Assert.IsTrue(urls.All(url=>url.StartsWith("https://www.youtube.com/watch?")));
            Debug.WriteLine($"Items: {urls.Count}");
            foreach (var url in urls) Debug.WriteLine(url);
        }

        [TestMethod]
        public void PlaylistPageExtractionTest() {
            var urls = DownloadUrlResolver.ExtractPlaylist(PlaylistPage);
            Assert.IsNotNull(urls);
            Assert.IsTrue(urls.Count > 0);
            Assert.IsTrue(urls.All(url => url.StartsWith("https://www.youtube.com/watch?")));
            Debug.WriteLine($"Items: {urls.Count}");
            foreach (var url in urls) Debug.WriteLine(url);
            
        }

    }
}