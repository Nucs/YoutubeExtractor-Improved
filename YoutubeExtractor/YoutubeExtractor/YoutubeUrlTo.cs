using System;
using System.IO;
using System.Linq;

namespace YoutubeExtractor.Interface {
    public static class YoutubeUrlTo {

        /// <summary>
        ///     Clears the invalid path character from a title to make it savable.
        /// </summary>
        public static string SaveName(string s) {
            return Path.GetInvalidFileNameChars().Aggregate(s, (current, c) => current.Replace(c.ToString(), ""));
        }

        public static string Normalized(string s) {
            if (DownloadUrlResolver.IsPlaylistUrl(s))
                return DownloadUrlResolver.NormalizeYoutubePlaylistUrl(s);
            return DownloadUrlResolver.NormalizeYoutubeUrl(s);
        }
    }
}