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

            // Get new URL
            var definedEngineUrl = Configuration.Search == SearchEngine.Custom
                ? Configuration.CustomQueryUrl : Configuration.GetEnumDescription(Configuration.Search);

            // Replace Search Engine
            encodedUrl = encodedUrl.Replace("https://www.bing.com/search?q=", definedEngineUrl);

#if DEBUG
            var uriMessageUi = new MessageUi("GoAwayEdge",
                "Got extracted and decoded url variable from Microsoft Edge Protocol:\n\n" + encodedUrl, "OK", isMainThread: true);
            uriMessageUi.ShowDialog();
#endif
            var uri = new Uri(encodedUrl);
            return uri.ToString();
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
