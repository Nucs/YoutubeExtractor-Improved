using System;
using System.IO;
using System.Linq;

namespace YoutubeExtractor.Interface {
    public static class YoutubeUrlTo {

/*        /// <summary>
        ///     Gives a full qualified url from any youtube variaty of url. Invalid returns null.
        /// </summary>
        public static string ParseUrl(string url) {
            if (url == null)
                return null;
            if (url.Contains("youtu.be"))
                url = url.Replace("youtu.be/", "youtube.com/watch?v=");

            if (!url.Contains("youtube.com") && url.Split('.')[0].Contains("youtube")) 
                return null;
            
            if (!url.Contains("www.youtube.com"))
                url = url.Replace("youtube.com", "www.youtube.com");
            if (url.StartsWith("http") == false)
                url = "https://" + url;

            if (url.Contains("?v=") == false)
                return null;

            if (url.Contains("&")) {
                return "https://www.youtube.com/watch?v=" + url.Split(new [] {"watch?v=", "&"}, StringSplitOptions.RemoveEmptyEntries)[1];
            }

            return url;
        }*/

        /// <summary>
        ///     Clears the invalid path character from a title to make it savable.
        /// </summary>
        public static string SaveName(string s) {
            return Path.GetInvalidFileNameChars().Aggregate(s, (current, c) => current.Replace(c.ToString(), ""));
        }

        public static string Normalized(string s) {
            return DownloadUrlResolver.NormalizeYoutubeUrl(s);
        }
    }
}