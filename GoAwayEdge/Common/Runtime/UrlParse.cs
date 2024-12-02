using System.Text;
using System.Web;

namespace GoAwayEdge.Common.Runtime
{
    public class UrlParse
    {
        /// <summary>
        /// Parses the given encoded URL and returns the processed URL.
        /// </summary>
        /// <param name="encodedUrl">The encoded URL to parse.</param>
        /// <returns>The parsed URL.</returns>
        public static string Parse(string encodedUrl)
        {
            // Remove URI handler with url argument prefix
            encodedUrl = encodedUrl[encodedUrl.IndexOf("http", StringComparison.Ordinal)..];

            // Remove junk after search term
            if (encodedUrl.Contains("https%3A%2F%2Fwww.bing.com%2Fsearch%3Fq%3D") && !encodedUrl.Contains("redirect"))
                encodedUrl = encodedUrl.Substring(encodedUrl.IndexOf("http", StringComparison.Ordinal), encodedUrl.IndexOf("%26", StringComparison.Ordinal));

            // Alternative url form
            if (encodedUrl.Contains("https%3A%2F%2Fwww.bing.com%2Fsearch%3Fform%3D"))
            {
                encodedUrl = encodedUrl.Substring(encodedUrl.IndexOf("26q%3D", StringComparison.Ordinal) + 6, encodedUrl.Length - (encodedUrl.IndexOf("26q%3D", StringComparison.Ordinal) + 6));
                encodedUrl = "https://www.bing.com/search?q=" + encodedUrl;
            }

            // Decode Url
            encodedUrl = encodedUrl.Contains("redirect") ? DotSlash(encodedUrl) : DecodeUrlString(encodedUrl);

            // Replace Search Engine
            encodedUrl = encodedUrl.Replace("https://www.bing.com/search?q=", DefineEngine(Configuration.Search));

#if DEBUG
            var uriMessageUi = new MessageUi("GoAwayEdge",
                "New Uri: " + encodedUrl, "OK", isMainThread: true);
            uriMessageUi.ShowDialog();
#endif
            var uri = new Uri(encodedUrl);
            return uri.ToString();
        }

        private static string DefineEngine(SearchEngine engine)
        {
            var customQueryUrl = string.Empty;
            try
            {
                customQueryUrl = RegistryConfig.GetKey("CustomQueryUrl");
            }
            catch
            {
                // ignore; not an valid object
            }

            return engine switch
            {
                SearchEngine.Google => "https://www.google.com/search?q=",
                SearchEngine.Bing => "https://www.bing.com/search?q=",
                SearchEngine.DuckDuckGo => "https://duckduckgo.com/?q=",
                SearchEngine.Yahoo => "https://search.yahoo.com/search?p=",
                SearchEngine.Yandex => "https://yandex.com/search/?text=",
                SearchEngine.Ecosia => "https://www.ecosia.org/search?q=",
                SearchEngine.Ask => "https://www.ask.com/web?q=",
                SearchEngine.Qwant => "https://qwant.com/?q=",
                SearchEngine.Perplexity => "https://www.perplexity.ai/search?copilot=false&q=",
                SearchEngine.Custom => customQueryUrl,
                _ => "https://www.google.com/search?q="
            };
        }

        private static string DecodeUrlString(string url)
        {
            string newUrl;
            while ((newUrl = Uri.UnescapeDataString(url)) != url)
                url = newUrl;
            return newUrl;
        }

        private static string DotSlash(string url)
        {
            string newUrl;
            while ((newUrl = Uri.UnescapeDataString(url)) != url)
                url = newUrl;

            try // Decode base64 string from url
            {
                var uri = new Uri(url);
                var query = HttpUtility.ParseQueryString(uri.Query).Get("url");
                if (query != null)
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(query));
                    return decoded;
                }
            }
            catch
            {
                // ignored
            }

            return url;
        }
    }
}
