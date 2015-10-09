using System;
using System.IO;

namespace YoutubeExtractor {

    /// <summary>
    ///     Provides the base class for the <see cref="AudioDownloader" /> and <see cref="VideoDownloader" /> class.
    /// </summary>
    public abstract class Downloader {
        protected readonly YoutubeContext context;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Downloader" /> class.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="video" /> or <paramref name="savePath" /> is <c>null</c>.</exception>
        /// <param name="video">The video to download/convert.</param>
        /// <param name="savePath">The path to save the video/audio.</param>
        protected Downloader(VideoInfo video, string savePath) {
            if (video == null)
                throw new ArgumentNullException(nameof(video));

            if (savePath == null)
                throw new ArgumentNullException(nameof(savePath));
            context = new YoutubeContext {
                VideoInfo = video,
                savepath = savePath
            };
        }
        /// <summary>
        ///     Initializes a new instance of the <see cref="Downloader" /> class.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="video" /> or <paramref name="savePath" /> is <c>null</c>.</exception>
        protected Downloader(YoutubeContext context) {
            this.context = context;
        }

        /// <summary>
        ///     Starts the work of the <see cref="Downloader" />.
        /// </summary>
        public abstract void Execute();
    }
}