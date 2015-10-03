using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace YoutubeExtractor {
    /// <summary>
    ///     Provides a method to download a video from YouTube.
    /// </summary>
    public class VideoDownloader : Downloader {
        private readonly HttpClient _httpClient;

        /// <summary>
        ///     Initializes a new instance of the <see cref="VideoDownloader" /> class.
        /// </summary>
        /// <param name="video">The video to download.</param>
        /// <param name="savePath">The path to save the video.</param>
        /// <param name="bytesToDownload">An optional value to limit the number of bytes to download.</param>
        /// <exception cref="ArgumentNullException"><paramref name="video" /> or <paramref name="savePath" /> is <c>null</c>.</exception>
        public VideoDownloader(VideoInfo video, string savePath, int? bytesToDownload = null)
            : base(video, savePath, bytesToDownload) {
            _httpClient = new HttpClient();
        }

        /// <summary>
        ///     Occurs when the downlaod progress of the video file has changed.
        /// </summary>
        public event EventHandler<ProgressEventArgs> DownloadProgressChanged;

        /// <summary>
        ///     Starts the video download.
        /// </summary>
        /// <exception cref="IOException">The video file could not be saved.</exception>
        /// <exception cref="WebException">An error occured while downloading the video.</exception>
        public override void Execute() {
            OnDownloadStarted(EventArgs.Empty);

            var request = (HttpWebRequest) WebRequest.Create(Video.DownloadUrl);

            if (BytesToDownload.HasValue)
                request.AddRange(0, BytesToDownload.Value - 1);

            // the following code is alternative, you may implement the function after your needs
            using (var response = request.GetResponse())
            using (var source = response.GetResponseStream())
            using (var target = File.Open(SavePath, FileMode.Create, FileAccess.Write)) {
                var buffer = new byte[1024];
                var cancel = false;
                int bytes;
                var copiedBytes = 0;

                while (!cancel && (bytes = source.Read(buffer, 0, buffer.Length)) > 0) {
                    target.Write(buffer, 0, bytes);

                    copiedBytes += bytes;

                    var eventArgs = new ProgressEventArgs((copiedBytes * 1.0 / response.ContentLength) * 100);

                    if (DownloadProgressChanged != null) {
                        DownloadProgressChanged(this, eventArgs);

                        if (eventArgs.Cancel)
                            cancel = true;
                    }
                }
            }

            OnDownloadFinished(EventArgs.Empty);
        }

        public async Task ExecuteAsync() {
            OnDownloadStarted(EventArgs.Empty);

            var response = await _httpClient.GetAsync(Video.DownloadUrl);
            if (!response.IsSuccessStatusCode)
                throw new Exception();

            using (var downloadStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = File.Open(SavePath, FileMode.Create, FileAccess.Write)) {
                var buffer = new byte[0x4000]; //16KB buffer
                var cancelRequest = false;

                int bytes;
                double bytesDownloaded = 0;

                while (!cancelRequest && (bytes = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                    await fileStream.WriteAsync(buffer, 0, bytes);
                    bytesDownloaded += bytes;

                    var eventArgs = new ProgressEventArgs((bytesDownloaded / downloadStream.Length) * 100);

                    DownloadProgressChanged?.Invoke(this, eventArgs);
                    if (eventArgs.Cancel)
                        cancelRequest = true;
                }
            }
        }
    }
}