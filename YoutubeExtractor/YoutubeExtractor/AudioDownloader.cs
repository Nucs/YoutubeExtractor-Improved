// ****************************************************************************
//
// FLV Extract
// Copyright (C) 2013-2014 Dennis Daume (daume.dennis@gmail.com)
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// ****************************************************************************

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MediaToolkit;
using MediaToolkit.Model;

namespace YoutubeExtractor {
    /// <summary>
    ///     Provides a method to download a video and extract its audio track.
    /// </summary>
    public class AudioDownloader : Downloader {
        private bool isCanceled;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AudioDownloader" /> class.
        /// </summary>
        /// <param name="video">The video to convert.</param>
        /// <param name="savePath">The path to save the audio.</param>
        /// ///
        /// <param name="bytesToDownload">An optional value to limit the number of bytes to download.</param>
        /// <exception cref="ArgumentNullException"><paramref name="video" /> or <paramref name="savePath" /> is <c>null</c>.</exception>
        public AudioDownloader(VideoInfo video, string savePath, int? bytesToDownload = null)
            : base(video, savePath, bytesToDownload) {}

        /// <summary>
        ///     Occurs when the progress of the audio extraction has changed.
        /// </summary>
        public event EventHandler<ProgressEventArgs> AudioExtractionProgressChanged;

        /// <summary>
        ///     Occurs when the download progress of the video file has changed.
        /// </summary>
        public event EventHandler<ProgressEventArgs> DownloadProgressChanged;

        /// <summary>
        ///     Downloads the video from YouTube and then extracts the audio track out if it.
        /// </summary>
        /// <exception cref="IOException">
        ///     The temporary video file could not be created.
        ///     - or -
        ///     The audio file could not be created.
        /// </exception>
        /// <exception cref="AudioExtractionException">An error occured during audio extraction.</exception>
        /// <exception cref="WebException">An error occured while downloading the video.</exception>
        public override void Execute() {
            var tempPath = Path.GetTempFileName();

            DownloadVideo(tempPath);

            if (!isCanceled)
                ExtractAudio(tempPath);

            OnDownloadFinished(EventArgs.Empty);
        }

        private void DownloadVideo(string path) {
            var videoDownloader = new VideoDownloader(Video, path, BytesToDownload);

            videoDownloader.DownloadProgressChanged += (sender, args) => {
                if (DownloadProgressChanged != null) {
                    DownloadProgressChanged(this, args);

                    isCanceled = args.Cancel;
                }
            };

            videoDownloader.Execute();
        }

        private void ExtractAudio(string path) {
            switch (Video.VideoType) {
                case VideoType.Mobile:
                    break;
                case VideoType.Flash:
                    using (var flvFile = new FlvFile(path, SavePath)) {
                        flvFile.ConversionProgressChanged += (sender, args) => { AudioExtractionProgressChanged?.Invoke(this, new ProgressEventArgs(args.ProgressPercentage)); };

                        flvFile.ExtractStreams();
                    }
                    break;
                case VideoType.Mp4:
                    var @in = new MediaFile(path);
                    var @out = new MediaFile(SavePath);
                    using (var engine = new Engine()) {
                        //Desperate attempt to catch the extraction from the MediaToolkit lib but it simply does not pass into those events.
                        //engine.ConvertProgressEvent += (sender, args) => AudioExtractionProgressChanged?.Invoke(this, new ProgressEventArgs((args.ProcessedDuration.TotalMilliseconds / args.TotalDuration.TotalMilliseconds) * 100f));
                        //engine.ConversionCompleteEvent += (sender, args) => AudioExtractionProgressChanged?.Invoke(this, new ProgressEventArgs((args.ProcessedDuration.TotalMilliseconds / args.TotalDuration.TotalMilliseconds) * 100f));
                        //informing on 0% and 100%, btw those conversions are pretty fast, 5 to 10 seconds for a 50MB 1048p video.
                        AudioExtractionProgressChanged?.Invoke(this, new ProgressEventArgs(0F));
                        engine.Convert(@in, @out); //begin conversion progress. it is executed serially.
                        AudioExtractionProgressChanged?.Invoke(this, new ProgressEventArgs(100f)); //invoke completed
                    }
                    break;
                case VideoType.WebM:
                    break;
                case VideoType.Unknown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task ExecuteAsync() {
            var tempPath = Path.GetTempFileName();

            await DownloadVideoAsync(tempPath);

            if (!isCanceled)
                ExtractAudio(tempPath);

            OnDownloadFinished(EventArgs.Empty);
        }

        private Task DownloadVideoAsync(string path) {
            var videoDownloader = new VideoDownloader(Video, path, BytesToDownload);

            videoDownloader.DownloadProgressChanged += (sender, args) => {
                if (DownloadProgressChanged != null) {
                    DownloadProgressChanged(this, args);

                    isCanceled = args.Cancel;
                }
            };

            return videoDownloader.ExecuteAsync();
        }
    }
}