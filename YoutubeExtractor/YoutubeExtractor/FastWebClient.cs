using System;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YoutubeExtractor {
    /// <summary>
    ///     A webclient with customized config to run 200% faster than normal webclient.
    /// </summary>
    public class FastWebClient : WebClient {

        public uint Timeout { get; set; } = 10000;

        static FastWebClient() {
            WebRequest.DefaultWebProxy = null;
            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            ServicePointManager.CheckCertificateRevocationList = false;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            int minWorker, minIOC;
            // Get the current settings.
            ThreadPool.GetMinThreads(out minWorker, out minIOC);
            // Change the minimum number of worker threads to four, but
            // keep the old setting for minimum asynchronous I/O 
            // completion threads.
            ThreadPool.SetMinThreads(50, minIOC); //50 threads to handle async tasks.
        }

        public FastWebClient() {
            this.Encoding = Encoding.UTF8;
            this.Proxy = null;
            //this.CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);
        }

        protected override WebRequest GetWebRequest(Uri address) {
            HttpWebRequest req = (HttpWebRequest)base.GetWebRequest(address);
            req.Timeout = Convert.ToInt32(Timeout);
            req.Proxy = null;
            return (WebRequest)req;
        }
    }

        /// <summary>
    ///     A webclient with customized config to run 200% faster than normal webclient.
    /// </summary>
    public class FastHttpClient : HttpClient {
        static FastHttpClient() {
            WebRequest.DefaultWebProxy = null;
            ServicePointManager.DefaultConnectionLimit = Int32.MaxValue;
            ServicePointManager.CheckCertificateRevocationList = false;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            
            int minWorker, minIOC;
            // Get the current settings.
            ThreadPool.GetMinThreads(out minWorker, out minIOC);
            // Change the minimum number of worker threads to four, but
            // keep the old setting for minimum asynchronous I/O 
            // completion threads.
            ThreadPool.SetMinThreads(50, minIOC); //50 threads to handle async tasks.
        }

        public FastHttpClient(int timeout) {
            if (timeout>0)
                this.Timeout = TimeSpan.FromMilliseconds(timeout);
        }

        public FastHttpClient() :this(-1) {
        }
    }

}