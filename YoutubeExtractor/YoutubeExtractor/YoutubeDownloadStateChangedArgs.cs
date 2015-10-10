namespace YoutubeExtractor {
    public class YoutubeDownloadStateChangedArgs {

        /// <summary>
        ///     Describes the current stage in processing the url.
        /// </summary>
        public YoutubeStage Stage { get; set; }

        /// <summary>
        ///     The progress to the current stage, works on Audio extraction and Downloading.
        /// </summary>
        public double Precentage { get; set; } = 0d;

        /// <summary>
        ///     Cancel Downloading flag
        /// </summary>
        public bool Cancel { get; set; } = false;

        /// <summary>
        ///     Flag for multiple listeners that handle the event to dispaly it in the UI.
        ///     If true, means that some event handler already took care of it.
        /// </summary>
        public bool UIHandled { get; set; } = false;

    }

    public enum YoutubeStage {
        Undefined = 0,
        ThumbnailFound,
        ProcessingUrls,
        DecipheringUrls,
        StartingDownload,
        Downloading,
        DownloadFinished,
        StartingAudioExtraction,
        ExtractingAudio,
        FinishedAudioExtraction,
        Completed
        
    }

}