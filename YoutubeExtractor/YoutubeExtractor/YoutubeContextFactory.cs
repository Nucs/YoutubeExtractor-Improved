/*using System;
using System.IO;
using System.Web.Configuration;
using YoutubeExtractor.Interface;

namespace YoutubeExtractor {
    public class YoutubeContextFactory {

        public string Url { get; set; } = null;
        public FileInfo VideoPath { get; set; } = null;
        public FileInfo AudioPath { get; set; } = null;
        public bool FetchThumbnail { get; set; } = false;

        public YoutubeContextFactory(string url) {
            Url = url;
        }

        /// <summary>
        ///     Variety of contexts that is connected to the User's directory. (e.g. MyDocuments)
        /// </summary>
        public static class UserDocuments {
            /// <summary>
            ///     Creates context that will save audio to My Music of the current user, deletes video.
            /// </summary>
            /// <param name="url"></param>
            /// <param name="loadthumbnail"></param>
            /// <param name="subdir">Subdir, for example ''youtube videos'' will result in My Videos\youtube videos</param>
            public static YoutubeContext AudioToMyAudio(string url, bool loadthumbnail = false, string subdir = null) {
                var context = new YoutubeContext(url, loadthumbnail);
                context.AudioPath = subdir == null ? new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)) : new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), subdir));
                return context;
            }

            /// <summary>
            ///     Creates context that will save Video to My Videos of the current user, deletes video.
            /// </summary>
            /// <param name="url"></param>
            /// <param name="loadthumbnail"></param>
            /// <param name="subdir">Subdir, for example ''youtube videos'' will result in My Videos\youtube videos</param>
            public static YoutubeContext VideoToMyVideo(string url, bool loadthumbnail = false, string subdir = null) {
                var context = new YoutubeContext(url, loadthumbnail);
                context.VideoPath = subdir == null ? new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)) : new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), subdir));
                return context;
            }

            /// <summary>
            ///     Creates context that will save Video to My Videos and audio to My Music of the current user, deletes video.
            /// </summary>
            /// <param name="url"></param>
            /// <param name="loadthumbnail"></param>
            /// <param name="subdir">Subdir, for example ''youtube videos'' will result in My Videos\youtube videos</param>
            /// <returns></returns>
            public static YoutubeContext VideoAndAudioToUserDocs(string url, bool loadthumbnail = false, string subdir = null) {
                var context = new YoutubeContext(url, loadthumbnail);
                context.VideoPath = subdir == null ? new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)) : new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), subdir));
                context.AudioPath = subdir == null ? new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)) : new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), subdir));
                return context;
            }
        }

        public YoutubeContext ToContext() {
            var c = new YoutubeContext(Url, FetchThumbnail);
            c.AudioPath = AudioPath;
            c.VideoPath = VideoPath;
            return c;
        } 
    }
}*/