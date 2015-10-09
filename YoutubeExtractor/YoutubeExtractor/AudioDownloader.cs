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
        public AudioDownloader(VideoInfo video, string savePath) : base(video, savePath) {}

        /// <summary>
        ///     Initializes a new instance of the <see cref="AudioDownloader" /> class.
        /// </summary>
        /// <param name="context">The current context</param>
        /// <param name="findBestVideoInfo">Will execute FindBestQualityVideoInfo to the current context</param>
        public AudioDownloader(YoutubeContext context, bool findBestVideoInfo = false) : base(context) {
            if (findBestVideoInfo)
                DownloadUrlResolver.FindHighestQualityDownloadUrl(context);
        }

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
                ExtractAudio(tempPath).Wait();
            context.OnProgresStateChanged(YoutubeStage.Completed);
        }

        private void DownloadVideo(string path) {
            context.VideoPath = new FileInfo(path);
            var vd = new VideoDownloader(context);

            vd.Execute();
        }

        private async Task ExtractAudio(string path) {
            var cache = new FileInfo(context.savableFilename); //target cache
            var SavePath = context.savableFilename;//to universal string
            for (int i = 1; File.Exists(SavePath); i++) {
                SavePath = Path.Combine(cache.Directory.FullName, $"{cache.Name.Replace(cache.Extension,"")} ({i}){cache.Extension}");
            }
            context.AudioPath = new FileInfo(SavePath);
            switch (context.VideoInfo.VideoType) {
                case VideoType.Mobile:
                    break;
                case VideoType.Flash:
                    using (var flvFile = new FlvFile(path, SavePath)) {
                        flvFile.ConversionProgressChanged += (sender, args) => {
                            context.OnProgresStateChanged(YoutubeStage.ExtractingAudio, args.ProgressPercentage);
                        };

                        await Task.Run(()=>flvFile.ExtractStreams());
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
                        context.OnProgresStateChanged(YoutubeStage.StartingAudioExtraction);
                        await Task.Run(()=>engine.Convert(@in, @out)); //begin conversion progress. it is executed serially.
                        context.OnProgresStateChanged(YoutubeStage.FinishedAudioExtraction);
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
        public async Task ExecuteAsync() {
            var tempPath = Path.GetTempFileName();

            await DownloadVideoAsync(tempPath);
            
            if (!isCanceled)
                await ExtractAudio(tempPath);

            context.OnProgresStateChanged(YoutubeStage.Completed);
        }

        private async Task DownloadVideoAsync(string path) {
            context.VideoPath = new FileInfo(path);
            var vd = new VideoDownloader(context);
            await vd.ExecuteAsync();
        }
    }
}