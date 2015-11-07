using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace YoutubeExtractor {
    /// <summary>
    ///     Handles logic for retrieving highest quality thumbnail picture for the given youtube url.
    ///     Ordered from highest quality to lowest.
    /// </summary>
    public class YoutubeThumbnail {
        private static readonly string[] qualitypics = {
            "https://i.ytimg.com/vi/{0}/sddefault.jpg",
            "https://i.ytimg.com/vi/{0}/hqdefault.jpg",
            "https://i.ytimg.com/vi/{0}/mqdefault.jpg",
            "https://i.ytimg.com/vi/{0}/default.jpg",
        };

        /// <summary>
        ///     Returns all possible thumbnail urls for the youtube key (youtube.com/watch?v={key}).
        ///     They are not confirmed to exist, for full verification construct new instace of <see cref="YoutubeThumbnail"/>
        /// </summary>
        /// <param name="key">The key used to identify a youtube video - youtube.com/watch?v={key}</param>
        public static string[] GetThumbnailsForKey(string key) {
            return qualitypics.Select(q => string.Format(q, key)).ToArray();
        }

        /// <summary>
        ///     If no valid thumbnail url is found, this is returned.
        /// </summary>
        public static string DefaultThumbnail { get; set; } = "Resources/default.png";
        
        private string _url { get; }
        private string _key { get; }

        private volatile string _thumbnail;
        private Task _thumbnail_task { get; set; }

        /// <summary>
        ///     The retrieved thumbnail from an asnyc task that is started the moment this object is constructed.     
        ///     If the task hasn't finished, it'll wait for it to finish.
        ///     This is never a null.
        /// </summary>
        /// <exception cref="Exception">Will throw is the thumbnail retrival has exception.</exception>
        public string Thumbnail {
            get {
                if (_thumbnail_task.IsFaulted)
                    throw (Exception) _thumbnail_task.Exception ?? new TaskCanceledException(); //incase..
                if (!_thumbnail_task.IsCompleted)
                    _thumbnail_task.Wait();
                return _thumbnail;
            }
        }

        /// <summary>
        ///     The retrieved thumbnail from an asnyc task that is started the moment this object is constructed.     
        ///     If the task hasn't finished, it'll return the path to default image, "Resources/default.png"
        /// </summary>
        /// <exception cref="Exception">Will throw is the thumbnail retrival has exception.</exception>
        public string SafeThumbnail {
            get {
                if (_thumbnail_task != null &&_thumbnail_task.IsFaulted)
                    throw (Exception) _thumbnail_task.Exception ?? new TaskCanceledException(); //incase..
                if (_thumbnail_task == null || !_thumbnail_task.IsCompleted)
                    return DefaultThumbnail;
                return _thumbnail;
            }
        }

        public YoutubeThumbnail(YoutubeContext context) : this(context.Url) {
            _thumbnail_task.ContinueWith(task => 
            context.OnProgresStateChanged(YoutubeStage.ThumbnailFound));
        }

        public YoutubeThumbnail(string yturl) {
            _url = DownloadUrlResolver.NormalizeYoutubeUrl(yturl);
            if (_url==null)
                throw new ArgumentNullException(nameof(yturl));
            _key = HttpUtility.ParseQueryString(new Uri(_url).Query)["v"];
            if (string.IsNullOrEmpty(_key))
                throw new ArgumentNullException(nameof(yturl));
            _thumbnail_task = _findBestQualityThumbnail();
        }

        private async Task _findBestQualityThumbnail() {
            //response from ytime varies on the user agent, usually 404 is thrown on invalid, but the default return is checked either way. has unique size of 120x80.
            using (var fw = new FastWebClient())
                foreach (var qurl in qualitypics) {
                    var u = string.Format(qurl, _key);
                    byte[] data;
                    try {
                        data = await fw.DownloadDataTaskAsync(u);
                    } catch (WebException we) when ((we.Response as HttpWebResponse)?.StatusCode==HttpStatusCode.NotFound) {
                        continue;
                    }
                    using (var ms = new MemoryStream(data)) 
                        using (var img = Image.FromStream(ms))
                            if (img.Size.Width == 120 && img.Width == 80) //Its the small default which makes it invalid
                                continue;
                
                    _thumbnail = u;
                    break;
                }
        }
    }
}