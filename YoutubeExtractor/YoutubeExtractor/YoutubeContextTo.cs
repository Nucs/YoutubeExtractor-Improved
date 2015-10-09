using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace YoutubeExtractor.Interface {
    public static class YoutubeContextExtensions {
        #region Serial
        /// <summary>
        ///     Downloads context to audio, requires Url, Optional - VideoInfo (Default: Highest Quality), Optional - BaseDirectory
        /// </summary>
        public static void ToAudio(this YoutubeContext context) {
            if (context==null)
                throw new ArgumentException(nameof(context));
            if (string.IsNullOrEmpty(context.Url))
                throw new ArgumentException(nameof(context.Url));
            
            if (context.VideoInfo == null)
                DownloadUrlResolver.FindHighestAudioQualityDownloadUrl(context);
            
            var ad = new AudioDownloader(context);
            ad.Execute();
        }

        /// <summary>
        ///     Downloads context to audio, requires Url, Optional - VideoInfo (Default: Highest Quality), Optional - BaseDirectory
        /// </summary>
        public static void ToVideo(this YoutubeContext context, VideoType customtype = VideoType.Mp4) {
            if (context==null)
                throw new ArgumentException(nameof(context));
            if (string.IsNullOrEmpty(context.Url))
                throw new ArgumentException(nameof(context.Url));
            
            if (context.VideoInfo == null)
                DownloadUrlResolver.FindHighestVideoQualityDownloadUrl(context, customtype);
            
            var vd = new VideoDownloader(context);
            vd.Execute();
        }

        #endregion

        #region Parallel

        /// <summary>
        ///     Downloads context to audio, requires Url, Optional - VideoInfo (Default: Highest Quality), Optional - BaseDirectory
        /// </summary>
        public async static Task ToAudioAsync(this YoutubeContext context) {
            if (context==null)
                throw new ArgumentException(nameof(context));
            if (string.IsNullOrEmpty(context.Url))
                throw new ArgumentException(nameof(context.Url));
            
            if (context.VideoInfo == null)
                await DownloadUrlResolver.FindHighestAudioQualityDownloadUrlAsync(context);

            var ad = new AudioDownloader(context);
            await ad.ExecuteAsync();
        }

        /// <summary>
        ///     Downloads context to audio, requires Url, Optional - VideoInfo (Default: Highest Quality), Optional - BaseDirectory
        /// </summary>
        public async static Task ToVideoAsync(this YoutubeContext context, VideoType customtype = VideoType.Mp4) {
            if (context==null)
                throw new ArgumentException(nameof(context));
            if (string.IsNullOrEmpty(context.Url))
                throw new ArgumentException(nameof(context.Url));
            
            if (context.VideoInfo == null)
                await DownloadUrlResolver.FindHighestVideoQualityDownloadUrlAsync(context, customtype);
            
            var vd = new VideoDownloader(context);
            await vd.ExecuteAsync();
        }

        #endregion

    }
}