namespace YoutubeExtractor {
    /// <summary>
    ///     The video type. Also known as video container.
    /// </summary>
    public enum VideoType {
        /// <summary>
        ///     The video type is unknown. This can occur if YoutubeExtractor is not up-to-date.
        /// </summary>
        Unknown = 0,
        /// <summary>
        ///     Video for mobile devices (3GP).
        /// </summary>
        Mobile,
        Flash,
        Mp4,
        WebM,
        VP8,
        VP9,

        
    }
}