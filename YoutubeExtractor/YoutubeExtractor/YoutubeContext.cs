using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using YoutubeExtractor.Interface;

namespace YoutubeExtractor {
    public class YoutubeContext {
        //unique id for every new context.
        private Guid _contextid = Guid.NewGuid();

        private string _url;
        public event EventHandler<YoutubeDownloadStateChangedArgs> ProgresStateChanged;
        public event EventHandler<RetryableProcessFailed> DownloadFailed;

        /// <summary>
        ///     Must be set as property initializer on the declaration of the context
        /// </summary>
        public bool _loadThumbnail { get; }

        /// <summary>
        ///     Url to the thumbnail of this video, highest quality available.
        /// </summary>
        public string Thumbnail => _thumbnailgetter?.SafeThumbnail ?? YoutubeThumbnail.DefaultThumbnail;

        protected YoutubeThumbnail _thumbnailgetter { get; set; }

        /// <summary>
        ///     The url to the youtube video.
        /// </summary>
        public string Url {
            get { return _url; }
            set {
                if (value.Equals(_url, StringComparison.InvariantCulture))
                    return;
                _url = value;
                if (_loadThumbnail)
                    _thumbnailgetter = new YoutubeThumbnail(this);
            }
        }

        /// <summary>
        ///     Unique key used to identify the song on youtube. (youtube.com/video?v={key})
        /// </summary>
        public string YoutubeKey => HttpUtility.ParseQueryString(new Uri(Url).Query)["v"];

        /// <summary>
        ///     Marks to any active progress to cancel.
        ///     If the context is completed, this has no effect.
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        ///     The chosen video info.
        /// </summary>
        public VideoInfo VideoInfo { get; set; } 

        /// <summary>
        ///     Path of the audio extracted from the video.
        /// </summary>
        public FileInfo AudioPath { get; set; }

        /// <summary>
        ///     The base directory used to store the audio path and thumbnail. sort of a cache dir.
        ///     Default is the directory of the exe.
        /// </summary>
        public DirectoryInfo BaseDirectory { get; set; }

        /// <summary>
        ///     The path to the downloaded video. Availability limited when using AudioDownloader, so it might be null.
        /// </summary>
        public FileInfo VideoPath { get; set; }

        /// <summary>
        ///     When downloading the video, this field is filled with the amount of bytes that are left to download.
        /// </summary>
        public int? BytesToDownload { get; set; }

        /// <summary>
        ///     The video title
        /// </summary>
        public string Title => this.VideoInfo?.Title ?? "";

        public YoutubeContext() { }

        /// <param name="url">The url to the youtube video</param>
        /// <param name="loadThumbnail">Determines whether a parallel task should run to fetch the thumbnail url, otherwise it'll never load.</param>
        public YoutubeContext(string url, bool loadThumbnail=false) {
            _loadThumbnail = loadThumbnail;
            Url = DownloadUrlResolver.NormalizeYoutubeUrl(url);
        }

        /// <summary>
        ///     Waits for the thumbnail getter to finish,
        ///     If one doesnt exist - simply returns.
        /// </summary>
        public void WaitForThumbnail() {
            var a = _thumbnailgetter?.Thumbnail;
        }
        
        /// <summary>
        ///     Later determined if its for audio or video.
        /// </summary>
        internal string savepath { get; set; }

        /// <summary>
        ///     Save path to extracted audio
        /// </summary>
        public string AudioSaveableFilename => Path.Combine(BaseDirectory?.FullName??"", YoutubeUrlTo.SaveName(Title)+VideoInfo?.AudioExtension);
        public string VideoSaveableFilename => Path.Combine(BaseDirectory?.FullName??"", YoutubeUrlTo.SaveName(Title)+VideoInfo?.VideoExtension);

        internal YoutubeDownloadStateChangedArgs OnProgresStateChanged(object sender, YoutubeDownloadStateChangedArgs e) {
            ProgresStateChanged?.Invoke(sender, e);
            return e;
        }


        internal YoutubeDownloadStateChangedArgs OnProgresStateChanged(YoutubeDownloadStateChangedArgs e) {
            ProgresStateChanged?.Invoke(this, e);
            return e;
        }

        internal YoutubeDownloadStateChangedArgs OnProgresStateChanged(YoutubeStage stage) {
            return OnProgresStateChanged(new YoutubeDownloadStateChangedArgs() {Stage=stage});
        }

        internal YoutubeDownloadStateChangedArgs OnProgresStateChanged(YoutubeStage stage, double precentage) {
            return OnProgresStateChanged(new YoutubeDownloadStateChangedArgs() {Stage=stage, Precentage = precentage});
        }

        internal void OnDownloadFailed(RetryableProcessFailed e) {
            DownloadFailed?.Invoke(this, e);
        }

        protected bool Equals(YoutubeContext other) {
            return _contextid.Equals(other._contextid) && string.Equals(_url, other._url) && Equals(VideoInfo, other.VideoInfo);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((YoutubeContext) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = _contextid.GetHashCode();
                hashCode = (hashCode * 397) ^ (_url?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (VideoInfo?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
    }
}
