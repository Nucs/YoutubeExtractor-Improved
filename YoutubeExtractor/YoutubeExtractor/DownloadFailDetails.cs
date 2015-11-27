using System;

namespace YoutubeExtractor {

    /// <summary>
    /// Passed when a a process that can be retried has failed.
    /// </summary>
    public class RetryableProcessFailed {
        /// <summary>
        ///     Dry description to what it is subjected to, for example "Url Download"
        /// </summary>
        public string Subject { get; set; } = "";

        /// <summary>
        ///     The exception that has caused to the fail
        /// </summary>
        public Exception Exception { get; set; } = null;

        /// <summary>
        ///     The number of retries that has already was performed
        /// </summary>
        public uint NumberOfTries { get; set; } = 0;

        /// <summary>
        ///     If yes, it will reattempt.
        /// </summary>
        public bool ShouldRetry { get; set; } = true;

        public object Tag { get; set; } = null;

        public RetryableProcessFailed() { }
        public RetryableProcessFailed(string subject) {
            this.Subject = subject;
        }


        /// <summary>
        ///     Reset the values (ShouldRetry) and increments NumberOfRetries
        /// </summary>
        public void Defaultize(Exception newException=null) {
            NumberOfTries++;
            Exception = newException;
            ShouldRetry = true;
        }
    }
}