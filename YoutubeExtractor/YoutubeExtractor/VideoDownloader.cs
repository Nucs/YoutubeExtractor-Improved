using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeExtractor {
    /// <summary>
    ///     Provides a method to download a video from YouTube.
    /// </summary>
    public class VideoDownloader : Downloader {
        private readonly FastHttpClient _httpClient = new FastHttpClient();

        /// <summary>
        ///     Initializes a new instance of the <see cref="VideoDownloader" /> class.
        /// </summary>
        /// <param name="video">The video to download.</param>
        /// <param name="savePath">The path to save the video.</param>
        /// <exception cref="ArgumentNullException"><paramref name="video" /> or <paramref name="savePath" /> is <c>null</c>.</exception>
        public VideoDownloader(VideoInfo video, string savePath) : base(video, savePath) {}

        /// <summary>
        ///     Initializes a new instance of the <see cref="VideoDownloader" /> class.
        /// </summary>
        public VideoDownloader(YoutubeContext context) : base(context) { }

        /// <summary>
        ///     Occurs when the downlaod progress of the video file has changed.
        /// </summary>

        /// <summary>
        ///     Starts the video download.
        /// </summary>
        /// <exception cref="IOException">The video file could not be saved.</exception>
        /// <exception cref="WebException">An error occured while downloading the video.</exception>
        public override void Execute() {
            context.OnProgresStateChanged(YoutubeStage.StartingDownload);
            if (context.VideoInfo==null) throw new InvalidOperationException("Cant extract audio when no VideoInfo is selected.");
            HttpWebRequest request;
            var rpf = new RetryableProcessFailed("Video Downloader") { Tag = context };
            retry:try {
                request = (HttpWebRequest)WebRequest.Create(context.VideoInfo.DownloadUrl);
            } catch (Exception e) {
                rpf.Defaultize(e);
                context.OnDownloadFailed(rpf);
                if (rpf.ShouldRetry)
                    goto retry;
                return;
            }
            if (context.BytesToDownload.HasValue)
                request.AddRange(0, context.BytesToDownload.Value - 1);

            context.VideoPath = new FileInfo(context.VideoPath?.FullName ?? context.videoSaveableFilename);
            using (var response = request.GetResponse())
            using (var source = response.GetResponseStream())
            using (var target = File.Open(context.VideoPath.FullName, FileMode.Create, FileAccess.Write)) {
                var buffer = new byte[1024];
                var cancel = false;
                int bytes;
                var copiedBytes = 0;

                while (!cancel && (bytes = source.Read(buffer, 0, buffer.Length)) > 0) {
                    target.Write(buffer, 0, bytes);

                    copiedBytes += bytes;

                    var e = context.OnProgresStateChanged(YoutubeStage.Downloading, (copiedBytes * 1.0 / response.ContentLength) * 100f);

                    if (e.Cancel)
                        cancel = true;
                }
            }
            context.OnProgresStateChanged(YoutubeStage.DownloadFinished);
        }

        public async Task ExecuteAsync() {
            context.OnProgresStateChanged(YoutubeStage.StartingDownload);
            if (context.VideoInfo == null) throw new InvalidOperationException("Cant extract audio when no VideoInfo is selected.");

            HttpResponseMessage response;
            var rpf = new RetryableProcessFailed("Video Downloader") {Tag=context};
            retry:try {
                response = await _httpClient.GetAsync(context.VideoInfo.DownloadUrl);

            } catch (Exception e) {
                rpf.Defaultize(e);
                context.OnDownloadFailed(rpf);
                if (rpf.ShouldRetry)
                    goto retry;
                return;
            }

            if (!response.IsSuccessStatusCode)
                throw new Exception();
            context.VideoPath = new FileInfo(context.VideoPath?.FullName ?? context.videoSaveableFilename);

            using (var downloadStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = File.Open(context.VideoPath.FullName, FileMode.Create, FileAccess.Write)) {
                var buffer = new byte[0x4000]; //16KB buffer
                var cancelRequest = false;

                int bytes;
                double bytesDownloaded = 0;

                while (!cancelRequest && (bytes = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                    await fileStream.WriteAsync(buffer, 0, bytes);
                    bytesDownloaded += bytes;

                    var e = context.OnProgresStateChanged(YoutubeStage.Downloading, ((bytesDownloaded / downloadStream.Length) * 100));

                    if (e.Cancel)
                        cancelRequest = true;
                }
            }
            context.OnProgresStateChanged(YoutubeStage.DownloadFinished);
        }
    }
}