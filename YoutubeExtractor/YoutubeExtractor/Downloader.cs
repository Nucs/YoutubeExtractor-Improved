using System;

namespace YoutubeExtractor {
    /// <summary>
    ///     Provides the base class for the <see cref="AudioDownloader" /> and <see cref="VideoDownloader" /> class.
    /// </summary>
    public abstract class Downloader {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Downloader" /> class.
        /// </summary>
        /// <param name="video">The video to download/convert.</param>
        /// <param name="savePath">The path to save the video/audio.</param>
        /// ///
        /// <param name="bytesToDownload">An optional value to limit the number of bytes to download.</param>
        /// <exception cref="ArgumentNullException"><paramref name="video" /> or <paramref name="savePath" /> is <c>null</c>.</exception>
        protected Downloader(VideoInfo video, string savePath, int? bytesToDownload = null) {
            if (video == null)
                throw new ArgumentNullException("video");

            if (savePath == null)
                throw new ArgumentNullException("savePath");

            Video = video;
            SavePath = savePath;
            BytesToDownload = bytesToDownload;
        }

        /// <summary>
        ///     Gets the number of bytes to download. <c>null</c>, if everything is downloaded.
        /// </summary>
        public int? BytesToDownload { get; private set; }

        /// <summary>
        ///     Gets the path to save the video/audio.
        /// </summary>
        public string SavePath { get; private set; }

        /// <summary>
        ///     Gets the video to download/convert.
        /// </summary>
        public VideoInfo Video { get; private set; }

        /// <summary>
        ///     Occurs when the download finished.
        /// </summary>
        public event EventHandler DownloadFinished;

        /// <summary>
        ///     Occurs when the download is starts.
        /// </summary>
        public event EventHandler DownloadStarted;

        /// <summary>
        ///     Starts the work of the <see cref="Downloader" />.
        /// </summary>
        public abstract void Execute();

        protected void OnDownloadFinished(EventArgs e) {
            if (DownloadFinished != null)
                DownloadFinished(this, e);
        }

        protected void OnDownloadStarted(EventArgs e) {
            if (DownloadStarted != null)
                DownloadStarted(this, e);
        }
    }
}