﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace YoutubeExtractor {
    /// <summary>
    ///     Provides a method to get the download link of a YouTube video.
    /// </summary>
    public static class DownloadUrlResolver {
        private const string RateBypassFlag = "ratebypass";
        private const int CorrectSignatureLength = 81;
        private const string SignatureQuery = "signature";
        private static FastWebClient _httpClient => new FastWebClient();

        /// <summary>
        ///     Decrypts the signature in the <see cref="VideoInfo.DownloadUrl" /> property and sets it
        ///     to the decrypted URL. Use this method, if you have decryptSignature in the
        ///     <see
        ///         cref="GetDownloadUrls" />
        ///     method set to false.
        /// </summary>
        /// <param name="videoInfo">The video info which's downlaod URL should be decrypted.</param>
        /// <exception cref="YoutubeParseException">
        ///     There was an error while deciphering the signature.
        /// </exception>
        public static void DecryptDownloadUrl(VideoInfo videoInfo) {
            var queries = HttpHelper.ParseQueryString(videoInfo.DownloadUrl);

            if (queries.ContainsKey(SignatureQuery)) {
                var encryptedSignature = queries[SignatureQuery];

                string decrypted;

                try {
                    decrypted = GetDecipheredSignature(videoInfo, videoInfo.HtmlPlayerVersion, encryptedSignature);
                } catch (Exception ex) {
                    throw new YoutubeParseException("Could not decipher signature", ex);
                }

                videoInfo.DownloadUrl = HttpHelper.ReplaceQueryStringParameter(videoInfo.DownloadUrl, SignatureQuery, decrypted);
                videoInfo.RequiresDecryption = false;
            }
        }


        /// <summary>
        ///     Decrypts the signature in the <see cref="VideoInfo.DownloadUrl" /> property and sets it
        ///     to the decrypted URL. Use this method, if you have decryptSignature in the
        ///     <see
        ///         cref="GetDownloadUrls" />
        ///     method set to false.
        /// </summary>
        /// <param name="videoInfo">The video info which's downlaod URL should be decrypted.</param>
        /// <exception cref="YoutubeParseException">
        ///     There was an error while deciphering the signature.
        /// </exception>
        public static async Task DecryptDownloadUrlAsync(VideoInfo videoInfo) {
            var queries = HttpHelper.ParseQueryString(videoInfo.DownloadUrl);

            if (queries.ContainsKey(SignatureQuery)) {
                var encryptedSignature = queries[SignatureQuery];

                string decrypted;

                try {
                    decrypted = await GetDecipheredSignatureAsync(videoInfo, videoInfo.HtmlPlayerVersion, encryptedSignature);
                } catch (Exception ex) {
                    throw new YoutubeParseException("Could not decipher signature", ex);
                }

                videoInfo.DownloadUrl = HttpHelper.ReplaceQueryStringParameter(videoInfo.DownloadUrl, SignatureQuery, decrypted);
                videoInfo.RequiresDecryption = false;
            }
        }

        /// <summary>
        ///     Gets a list of <see cref="VideoInfo" />s for the specified URL.
        /// </summary>
        /// <param name="context">The context, must contain a url to the video in the Url property</param>
        /// <param name="decryptSignature">
        ///     A value indicating whether the video signatures should be decrypted or not. Decrypting
        ///     consists of a HTTP request for each <see cref="VideoInfo" />, so you may want to set
        ///     this to false and call <see cref="DecryptDownloadUrl" /> on your selected
        ///     <see
        ///         cref="VideoInfo" />
        ///     later.
        /// </param>
        /// <returns>A list of <see cref="VideoInfo" />s that can be used to download the video.</returns>
        /// <exception cref="VideoNotAvailableException">The video is not available.</exception>
        /// <exception cref="WebException">
        ///     An error occurred while downloading the YouTube page html.
        /// </exception>
        /// <exception cref="YoutubeParseException">The Youtube page could not be parsed.</exception>
        public static IEnumerable<VideoInfo> GetDownloadUrls(YoutubeContext context, bool decryptSignature = true) {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Url == null)
                throw new ArgumentNullException(nameof(context.Url));
            context.OnProgresStateChanged(YoutubeStage.ProcessingUrls);
            string ytb;
            var isYoutubeUrl = TryNormalizeYoutubeUrl(context.Url, out ytb);
            context.Url = ytb;
            if (!isYoutubeUrl)
                throw new ArgumentException("URL is not a valid youtube URL!");
            _retry:
            try {
                var rpf = new RetryableProcessFailed("ParseHtml5Version") {Tag = context.Url};
                _redownload:
                var json = LoadJson(context.Url);
                var videoTitle = GetVideoTitle(json);
                var n = 0;
                var downloadUrls = ExtractDownloadUrls(json);
                var infos = GetVideoInfos(downloadUrls, videoTitle, context.Url).ToArray();

                try {
                    var htmlPlayerVersion = GetHtml5PlayerVersion(json);


                    foreach (var info in infos) {
                        info.HtmlPlayerVersion = htmlPlayerVersion;

                        if (decryptSignature && info.RequiresDecryption)
                            DecryptDownloadUrl(info);
                    }

                    return infos;
                } catch (Exception e) {
                    rpf.Defaultize(e);
                    context.OnDownloadFailed(rpf);
                    Console.WriteLine(e);
                    if (rpf.ShouldRetry)
                        goto _redownload;
                    return null;
                }
            } catch (Exception ex) when (ex.Message == "Result cannot be called on a failed Match.") {
                goto _retry;
            } catch (Exception ex) {
                if (ex is WebException || ex is VideoNotAvailableException)
                    throw;

                ThrowYoutubeParseException(ex, context.Url);
            } //Message 

            return null; // Will never happen, but the compiler requires it
        }

        /// <summary>
        ///     Gets a list of <see cref="VideoInfo" />s for the specified URL.
        /// </summary>
        /// <param name="url">The youtube url</param>
        /// <param name="decryptSignature">
        ///     A value indicating whether the video signatures should be decrypted or not. Decrypting
        ///     consists of a HTTP request for each <see cref="VideoInfo" />, so you may want to set
        ///     this to false and call <see cref="DecryptDownloadUrl" /> on your selected
        ///     <see
        ///         cref="VideoInfo" />
        ///     later.
        /// </param>
        /// <returns>A list of <see cref="VideoInfo" />s that can be used to download the video.</returns>
        /// <exception cref="VideoNotAvailableException">The video is not available.</exception>
        /// <exception cref="WebException">
        ///     An error occurred while downloading the YouTube page html.
        /// </exception>
        /// <exception cref="YoutubeParseException">The Youtube page could not be parsed.</exception>
        public static IEnumerable<VideoInfo> GetDownloadUrls(string url, bool decryptSignature = true) {
            return GetDownloadUrls(new YoutubeContext(url, false), decryptSignature);
        }

        /// <summary>
        ///     Gets a list of <see cref="VideoInfo" />s for the specified URL.
        /// </summary>
        /// <param name="context">The context, must contain a url to the video in the Url property</param>
        /// <param name="decryptSignature">
        ///     A value indicating whether the video signatures should be decrypted or not. Decrypting
        ///     consists of a HTTP request for each <see cref="VideoInfo" />, so you may want to set
        ///     this to false and call <see cref="DecryptDownloadUrl" /> on your selected
        ///     <see
        ///         cref="VideoInfo" />
        ///     later.
        /// </param>
        /// <returns>A list of <see cref="VideoInfo" />s that can be used to download the video.</returns>
        /// <exception cref="VideoNotAvailableException">The video is not available.</exception>
        /// <exception cref="WebException">
        ///     An error occurred while downloading the YouTube page html.
        /// </exception>
        /// <exception cref="YoutubeParseException">The Youtube page could not be parsed.</exception>
        public static async Task<IEnumerable<VideoInfo>> GetDownloadUrlsAsync(YoutubeContext context, bool decryptSignature = true) {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Url == null)
                throw new ArgumentNullException(nameof(context.Url));

            context.OnProgresStateChanged(YoutubeStage.ProcessingUrls);
            string ytb;
            var isYoutubeUrl = TryNormalizeYoutubeUrl(context.Url, out ytb);
            context.Url = ytb;

            if (!isYoutubeUrl)
                throw new ArgumentException("URL is not a valid youtube URL!");

            try {
                var rpf = new RetryableProcessFailed("ParseHtml5Version") {Tag = context.Url};
                _redownload:
                var json = await LoadJsonAsync(context.Url);
                var videoTitle = GetVideoTitle(json);
                var n = 0;
                var downloadUrls = ExtractDownloadUrls(json);
                var infos = GetVideoInfos(downloadUrls, videoTitle, context.Url);
                string htmlPlayerVersion;

                try {
                    htmlPlayerVersion = GetHtml5PlayerVersion(json);
                } catch (Exception e) {
                    rpf.Defaultize(e);
                    context.OnDownloadFailed(rpf);
                    if (rpf.ShouldRetry)
                        goto _redownload;
                    return null;
                }

                foreach (var info in infos) {
                    info.HtmlPlayerVersion = htmlPlayerVersion;

                    if (decryptSignature && info.RequiresDecryption)
                        await DecryptDownloadUrlAsync(info);
                }

                return infos;
            } catch (Exception ex) {
                if (ex is WebException || ex is VideoNotAvailableException)
                    throw;

                ThrowYoutubeParseException(ex, context.Url);
            }

            return null; // Will never happen, but the compiler requires it
        }

        #region Highest Audio Quality Getters

        private static IEnumerable<VideoInfo> _orderByQuality(this IEnumerable<VideoInfo> infos) {
            return infos.Where(vid => vid.CanExtractAudio).OrderByDescending(info => info.AudioBitrate).ThenBy(info => info.VideoType).ToArray();
        }

        /// <summary>
        ///     Returns VideoInfo of the highest convertiable url of this youtube video based on given context
        /// </summary>
        public static VideoInfo GetHighestAudioQualityDownloadUrl(YoutubeContext context) {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrEmpty(context.Url))
                throw new ArgumentNullException(nameof(context.Url));
            var urls = GetDownloadUrls(context)._orderByQuality();
            var video = urls.FirstOrDefault();

            if (video?.RequiresDecryption == true)
                DecryptDownloadUrl(video);
            return video;
        }

        /// <summary>
        ///     Returns VideoInfo of the highest convertiable url of this youtube video
        /// </summary>
        public static async Task<VideoInfo> GetHighestAudioQualityDownloadUrlAsync(YoutubeContext context) {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (string.IsNullOrEmpty(context.Url))
                throw new ArgumentNullException(nameof(context.Url));
            var urls = (await GetDownloadUrlsAsync(context))._orderByQuality();
            var video = urls.FirstOrDefault();

            if (video?.RequiresDecryption == true)
                DecryptDownloadUrl(video);
            return video;
        }

        /// <summary>
        ///     Returns VideoInfo of the highest convertiable url of this youtube video based on given url
        /// </summary>
        public static VideoInfo GetHighestAudioQualityDownloadUrl(string url) {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));
            return GetHighestAudioQualityDownloadUrl(new YoutubeContext(url));
        }

        /// <summary>
        ///     Returns VideoInfo of the highest convertiable url of this youtube video
        /// </summary>
        public static async Task<VideoInfo> GetHighestAudioQualityDownloadUrlAsync(string url) {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));
            return await GetHighestAudioQualityDownloadUrlAsync(new YoutubeContext(url));
        }

        /// <summary>
        ///     Returns VideoInfo of the highest convertiable url of this youtube video
        /// </summary>
        public static void FindHighestAudioQualityDownloadUrl(this YoutubeContext context) {
            context.VideoInfo = GetHighestAudioQualityDownloadUrl(context);
        }

        /// <summary>
        ///     Returns VideoInfo of the highest convertiable url of this youtube video
        /// </summary>
        public static async Task FindHighestAudioQualityDownloadUrlAsync(this YoutubeContext context) {
            context.VideoInfo = await GetHighestAudioQualityDownloadUrlAsync(context);
        }

        #endregion

        #region Highest Video Quality Getters

        /// <summary>
        ///     Returns VideoInfo of the highest convertiable url of this youtube video
        /// </summary>
        public static VideoInfo GetHighestVideoQualityDownloadUrl(string url, VideoType type = VideoType.Mp4) {
            if (url == null || NormalizeYoutubeUrl(url) == null)
                throw new ArgumentNullException(nameof(url));
            var urls = GetDownloadUrls(new YoutubeContext(url));
            var video = urls.Where(vi => vi.VideoType == type).OrderByDescending(info => info.Resolution).FirstOrDefault();

            if (video?.RequiresDecryption == true)
                DecryptDownloadUrl(video);

            return video;
        }

        /// <summary>
        ///     Returns VideoInfo of the highest convertiable url of this youtube video
        /// </summary>
        public static async Task<VideoInfo> GetHighestVideoQualityDownloadUrlAsync(string url, VideoType type = VideoType.Mp4) {
            if (url == null || NormalizeYoutubeUrl(url) == null)
                throw new ArgumentNullException(nameof(url));
            var urls = await GetDownloadUrlsAsync(new YoutubeContext(url));
            var video = urls.Where(vi => vi.VideoType == type).OrderByDescending(info => info.Resolution).FirstOrDefault();

            if (video?.RequiresDecryption == true)
                DecryptDownloadUrl(video);
            return video;
        }

        /// <summary>
        ///     Returns VideoInfo of the highest convertiable url of this youtube video
        /// </summary>
        public static void FindHighestVideoQualityDownloadUrl(this YoutubeContext context, VideoType type = VideoType.Mp4) {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var urls = GetDownloadUrls(context);
            context.VideoInfo = urls.Where(vi => vi.VideoType == type).OrderByDescending(info => info.Resolution).FirstOrDefault();

            if (context.VideoInfo?.RequiresDecryption == true)
                DecryptDownloadUrl(context.VideoInfo);
        }

        /// <summary>
        ///     Returns VideoInfo of the highest convertiable url of this youtube video
        /// </summary>
        public static async Task FindHighestVideoQualityDownloadUrlAsync(this YoutubeContext context, VideoType type = VideoType.Mp4) {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var urls = await GetDownloadUrlsAsync(context);
            context.VideoInfo = urls.Where(vi => vi.VideoType == type).OrderByDescending(info => info.Resolution).FirstOrDefault();

            if (context.VideoInfo?.RequiresDecryption == true)
                DecryptDownloadUrl(context.VideoInfo);
        }

        #endregion

#if PORTABLE

        public static System.Threading.Tasks.Task<IEnumerable<VideoInfo>> GetDownloadUrlsAsync(string videoUrl, bool decryptSignature = true)
        {
            return System.Threading.Tasks.Task.Run(() => GetDownloadUrls(videoUrl, decryptSignature));
        }

#endif

        /// <summary>
        ///     Normalizes the given YouTube URL to the format http://youtube.com/watch?v={youtube-id}
        ///     and returns whether the normalization was successful or not.
        /// </summary>
        /// <param name="url">The YouTube URL to normalize.</param>
        /// <param name="normalizedUrl">The normalized YouTube URL.</param>
        /// <returns>
        ///     <c>true</c>, if the normalization was successful; <c>false</c>, if the URL is invalid.
        /// </returns>
        public static bool TryNormalizeYoutubeUrl(string url, out string normalizedUrl) {
            if (string.IsNullOrEmpty(url)) {
                normalizedUrl = null;
                return false;
            }
            url = url.Trim();
            var pos = url.LastIndexOf("#", StringComparison.InvariantCultureIgnoreCase);
            if (pos > 0)
                url = url.Substring(0, pos);

            if (url.Contains("youtu.be/") && url.Contains("list=")) {
                var keys = url.Split(new[] {".be/", "?list=", "#"}, StringSplitOptions.RemoveEmptyEntries);
                if (keys.Length < 3) {
                    normalizedUrl = null;
                    return false;
                }
                normalizedUrl = $"https://www.youtube.com/watch?v={keys[1]}";
                return true;
            }

            url = url.Replace("youtu.be/", "youtube.com/watch?v=");
            url = url.Replace("www.youtube", "youtube");
            url = url.Replace("youtube.com/embed/", "youtube.com/watch?v=");

            if (url.Contains("/v/"))
                url = "http://youtube.com" + new Uri(url).AbsolutePath.Replace("/v/", "/watch?v=");

            url = url.Replace("/watch#", "/watch?");

            var query = HttpHelper.ParseQueryString(url);

            string v;

            if (!query.TryGetValue("v", out v)) {
                normalizedUrl = null;
                return false;
            }

            normalizedUrl = "https://www.youtube.com/watch?v=" + v;

            return true;
        }


        /// <summary>
        ///     Normalizes the given YouTube URL to the format http://youtube.com/watch?v={youtube-id}
        ///     and returns whether the normalization was successful or not.
        /// </summary>
        /// <param name="url">The YouTube URL to normalize.</param>
        /// <returns>
        ///     <c>url</c>, if the normalization was successful; <c>null</c>, if the URL is invalid.
        /// </returns>
        public static string NormalizeYoutubeUrl(string url) {
            string @out;
            return !TryNormalizeYoutubeUrl(url, out @out) ? null : @out;
        }

        private static IEnumerable<ExtractionInfo> ExtractDownloadUrls(JObject json) {
            var splitByUrls = GetStreamMap(json).Split(',');
            var adaptiveFmtSplitByUrls = GetAdaptiveStreamMap(json).Split(',');
            if (!string.IsNullOrWhiteSpace(adaptiveFmtSplitByUrls[0]))
                splitByUrls = splitByUrls.Concat(adaptiveFmtSplitByUrls).Distinct().ToArray();

            foreach (var s in splitByUrls) {
                var queries = HttpHelper.ParseQueryString(s);
                string url;

                var requiresDecryption = false;

                if (queries.ContainsKey("s") || queries.ContainsKey("sig")) {
                    requiresDecryption = queries.ContainsKey("s");
                    var signature = queries.ContainsKey("s") ? queries["s"] : queries["sig"];

                    url = $"{queries["url"]}&{SignatureQuery}={signature}";

                    var fallbackHost = queries.ContainsKey("fallback_host") ? "&fallback_host=" + queries["fallback_host"] : string.Empty;

                    url += fallbackHost;
                } else
                    url = queries["url"];

                url = HttpHelper.UrlDecode(url);
                url = HttpHelper.UrlDecode(url);

                var parameters = HttpHelper.ParseQueryString(url);
                if (!parameters.ContainsKey(RateBypassFlag))
                    url += $"&{RateBypassFlag}={"yes"}";

                yield return new ExtractionInfo {RequiresDecryption = requiresDecryption, Uri = new Uri(url)};
            }
        }

        private static string GetAdaptiveStreamMap(JObject json) {
            JToken streamMap = json["args"]["adaptive_fmts"] ?? json["args"]["url_encoded_fmt_stream_map"];
            // bugfix: adaptive_fmts is missing in some videos, use url_encoded_fmt_stream_map instead

            return streamMap.ToString();
        }

        private static string GetDecipheredSignature(VideoInfo videoinfo, string htmlPlayerVersion, string signature) {
            return Decipherer.DecipherWithVersion(videoinfo, signature, htmlPlayerVersion);
        }

        private static async Task<string> GetDecipheredSignatureAsync(VideoInfo videoinfo, string htmlPlayerVersion, string signature) {
            return await Decipherer.DecipherWithVersionAsync(videoinfo, signature, htmlPlayerVersion);
        }

        private static string GetHtml5PlayerVersion(JObject json) {
            var regex = new Regex(@"player-(.+?).js");
            var js = json["assets"]["js"].ToString();
            return regex.Match(js).Result("$1");
        }

        private static string GetStreamMap(JObject json) {
            var streamMap = json["args"]["url_encoded_fmt_stream_map"];

            var streamMapString = streamMap?.ToString();

            if (streamMapString == null || streamMapString.Contains("been+removed"))
                throw new VideoNotAvailableException("Video is removed or has an age restriction.");

            return streamMapString;
        }

        private static IEnumerable<VideoInfo> GetVideoInfos(IEnumerable<ExtractionInfo> extractionInfos, string videoTitle, string videoUrl) {
            var downLoadInfos = new List<VideoInfo>();

            foreach (var extractionInfo in extractionInfos) {
                var itag = HttpHelper.ParseQueryString(extractionInfo.Uri.Query)["itag"];

                var formatCode = int.Parse(itag);

                var info = VideoInfo.Defaults.SingleOrDefault(videoInfo => videoInfo.FormatCode == formatCode);

                if (info != null)
                    info = new VideoInfo(info) {
                        DownloadUrl = extractionInfo.Uri.ToString(),
                        Title = videoTitle,
                        RequiresDecryption = extractionInfo.RequiresDecryption,
                        YoutubeUrl = videoUrl
                    };

                else
                    info = new VideoInfo(formatCode) {
                        DownloadUrl = extractionInfo.Uri.ToString(),
                        YoutubeUrl = videoUrl
                    };

                downLoadInfos.Add(info);
            }

            return downLoadInfos;
        }

        private static string GetVideoTitle(JObject json) {
            var title = json["args"]["title"];

            return title?.ToString() ?? string.Empty;
        }

        private static bool IsVideoUnavailable(string pageSource) {
            const string unavailableContainer = "<div id=\"watch-player-unavailable\">";

            return pageSource.Contains(unavailableContainer);
        }

        private static JObject LoadJson(string url) {
            string pageSource;
            var rpf = new RetryableProcessFailed("LoadUrls") {Tag = url};
            var timeout = 1500u;
            retry:
            try {
                pageSource = HttpHelper.DownloadString(url, timeout);
            } catch (Exception e) {
                rpf.Defaultize(e);
                //TODO FailedDownload?.Invoke(rpf);
                
                if (rpf.ShouldRetry) {
                    timeout += 500;
                    goto retry;
                }
                return null;
            }

            if (IsVideoUnavailable(pageSource))
                throw new VideoNotAvailableException();

            var dataRegex = new Regex(@"ytplayer\.config\s*=\s*(\{.+?\});", RegexOptions.Multiline);

            var extractedJson = dataRegex.Match(pageSource).Result("$1");

            return JObject.Parse(extractedJson);
        }

        private static async Task<JObject> LoadJsonAsync(string url) {
            string pageSource;
            var rpf = new RetryableProcessFailed("LoadUrls") {Tag = url};
            retry:
            try {
                pageSource = await _httpClient.DownloadStringTaskAsync(url);
            } catch (Exception e) {
                rpf.Defaultize(e);
                //TODO FailedDownload?.Invoke(rpf);
                if (rpf.ShouldRetry)
                    goto retry;
                return null;
            }

            if (IsVideoUnavailable(pageSource))
                throw new VideoNotAvailableException();

            var dataRegex = new Regex(@"ytplayer\.config\s*=\s*(\{.+?\});", RegexOptions.Multiline);

            var extractedJson = dataRegex.Match(pageSource).Result("$1");

            return JObject.Parse(extractedJson);
        }

        private static void ThrowYoutubeParseException(Exception innerException, string videoUrl) {
            throw new YoutubeParseException("Could not parse the Youtube page for URL " + videoUrl + "\n" +
                                            "This may be due to a change of the Youtube page structure.\n" +
                                            "Please report this bug at www.github.com/flagbug/YoutubeExtractor/issues", innerException);
        }

        /// <summary>
        ///     Extracts the playlist from the url, wether its on a playlist page or a side playlist when playing a video.
        ///     Will return the urls of the songs
        /// </summary>
        public static List<string> ExtractPlaylist(string url) {
            url = NormalizeYoutubePlaylistUrl(url);
            if (string.IsNullOrEmpty(url) || !IsPlaylistUrl(url))
                throw new ArgumentNullException(nameof(url));

            string pageSource;
            var rpf = new RetryableProcessFailed("LoadUrls") {Tag = url};
            retry:
            try {
                pageSource = _httpClient.DownloadString(url);
            } catch (Exception e) {
                rpf.Defaultize(e);
                //TODO FailedDownload?.Invoke(rpf);
                if (rpf.ShouldRetry)
                    goto retry;
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(pageSource);
            return _extractPlaylistUrls(url, doc);
        }

        /// <summary>
        ///     Extracts the playlist from the url, wether its on a playlist page or a side playlist when playing a video.
        ///     Will return the urls of the songs
        /// </summary>
        public static async Task<List<string>> ExtractPlaylistAsync(string url) {
            url = NormalizeYoutubePlaylistUrl(url);
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));

            string pageSource;
            var rpf = new RetryableProcessFailed("LoadUrls") {Tag = url};
            retry:
            try {
                pageSource = await _httpClient.DownloadStringTaskAsync(url);
            } catch (Exception e) {
                rpf.Defaultize(e);
                //TODO FailedDownload?.Invoke(rpf);
                if (rpf.ShouldRetry)
                    goto retry;
                return null;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(pageSource);
            return _extractPlaylistUrls(url, doc);
        }

        private static List<string> _extractPlaylistUrls(string url, HtmlDocument doc) {
            if (url.Contains("v=") && url.Contains("index=") && url.Contains("list=")) { //song playing with a side-playlist
                var nodes = doc.DocumentNode.SelectNodes(@"//a[contains(@href,'watch?v')][contains(@href,'list')][contains(@href,'index')]/@href")
                    .Select(a => "https://www.youtube.com" + HttpUtility.HtmlDecode(a.Attributes["href"].Value))
                    .Distinct()
                    .ToList();
                return nodes;
            }
            if (url.Contains("/playlist?")) { //a playlist
                var nodes = doc.DocumentNode.SelectNodes(@"//a[contains(@href,'watch?v')][contains(@href,'list')][contains(@href,'index')]/@href")
                    .Select(a => "https://www.youtube.com" + HttpUtility.HtmlDecode(a.Attributes["href"].Value))
                    .Distinct()
                    .ToList();
                return nodes;
            }

            //last attempt
            return doc.DocumentNode.SelectNodes(@"//a[contains(@href,'watch?v')]/@href")
                .Select(a => "https://www.youtube.com" + HttpUtility.HtmlDecode(a.Attributes["href"].Value))
                .Distinct()
                .ToList();
        }

        /// <summary>
        ///     Checks if the url is a playlist that can be extracted.
        /// </summary>
        public static bool IsPlaylistUrl(string url) {
            return url.Contains("list=");
        }

        /// <summary>
        ///     Normalizes the given YouTube URL to the format http://youtube.com/watch?v={youtube-id}&list={yotube-list-id}&index
        ///     ={song-index}
        ///     and returns whether the normalization was successful or not.
        /// </summary>
        /// <param name="url">The YouTube URL to normalize.</param>
        /// <param name="normalizedUrl">The normalized YouTube URL.</param>
        /// <returns>
        ///     <c>true</c>, if the normalization was successful; <c>false</c>, if the URL is invalid.
        /// </returns>
        public static bool TryNormalizeYoutubePlaylistUrl(string url, out string normalizedUrl) {
            url = url.Trim();
            var pos = url.LastIndexOf("#", StringComparison.InvariantCultureIgnoreCase);
            if (pos > 0)
                url = url.Substring(0, pos);
            if (url.Contains("youtu.be/") && url.Contains("list=")) {
                var keys = url.Split(new[] {".be/", "?list=", "#"}, StringSplitOptions.RemoveEmptyEntries);
                if (keys.Length < 3) {
                    normalizedUrl = null;
                    return false;
                }
                var _v = keys[1];
                var _list = keys[2];
                normalizedUrl = $"https://www.youtube.com/watch?v={_v}&list={_list}";
                return true;
            }
            //https://youtu.be/hT_nvWreIhg?list=RDebXbLfLACGM
            url = url.Replace("www.youtube", "youtube");

            if (url.Contains("/v/"))
                url = "http://youtube.com" + new Uri(url).AbsolutePath.Replace("/v/", "/watch?v=");

            url = url.Replace("/watch#", "/watch?");

            var query = HttpHelper.ParseQueryString(url);

            string v;
            var vtry = query.TryGetValue("v", out v);

            string list;
            var listtry = query.TryGetValue("list", out list);

            string index;
            query.TryGetValue("index", out index);

            if (!vtry && listtry) { //a plalist page.
                normalizedUrl = $"https://www.youtube.com/playlist?list={list}";
                return true;
            }

            if (!vtry || !listtry) { //not playlist or regular video
                normalizedUrl = null;
                return false;
            }

            normalizedUrl = $"https://www.youtube.com/watch?v={v}&list={list}" + (string.IsNullOrEmpty(index) ? "" : $"&index={index}");

            return true;
        }


        /// <summary>
        ///     Normalizes the given YouTube URL to the format http://youtube.com/watch?v={youtube-id}&list={yotube-list-id}&index
        ///     ={song-index}
        ///     and returns the normalized string if was successful. otherwise null.
        /// </summary>
        /// <param name="url">The YouTube URL to normalize.</param>
        /// <returns>
        ///     <c>string</c>, if the normalization was successful; <c>null</c>, if the URL is invalid.
        /// </returns>
        public static string NormalizeYoutubePlaylistUrl(string url) {
            string @out;
            return !TryNormalizeYoutubePlaylistUrl(url, out @out) ? null : @out;
        }

        private class ExtractionInfo {
            public bool RequiresDecryption { get; set; }

            public Uri Uri { get; set; }
        }
    }
}