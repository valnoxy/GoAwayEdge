using System.Globalization;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace GoAwayEdge.Common.Runtime
{
    public class Weather
    {
        public class GeoData
        {
            public string L { get; set; } // City
            public string R { get; set; } // Region
            public string C { get; set; } // Country
            public string I { get; set; } // Short Country code
            public string G { get; set; } // Country code (lowercase)
            public double X { get; set; } // longitude
            public double Y { get; set; } // latitude
        }

        public static string? ParseUrl(string encodedUrl)
        {
            // Encoded URL structure: https://msn.com/<country-code>/.../?loc=<base64-geolocation>
            // This function will locate the argument "?loc", decode the base64 geolocation information and format the template

            if (Configuration.WeatherProvider == WeatherProvider.Default)
                return encodedUrl;

            var encodedUri = new Uri(encodedUrl);
            var locValue = HttpUtility.ParseQueryString(encodedUri.Query).Get("loc"); // Output: Base64
            if (string.IsNullOrEmpty(locValue))
                return "";

            var encodedLocValue = Encoding.UTF8.GetString(Convert.FromBase64String(locValue)); // Output: Json 
            var locObject = JsonConvert.DeserializeObject<GeoData>(encodedLocValue);
            var language = Thread.CurrentThread.CurrentCulture.ToString(); // Country code (full case)
            if (locObject == null)
                return "";
                    
            var lat = locObject.Y.ToString(CultureInfo.InvariantCulture).Replace(",", ".");
            var lon = locObject.X.ToString(CultureInfo.InvariantCulture).Replace(",", ".");
            var placeholders = new Dictionary<string, string>
            {
                { "country-code", language },
                { "short-country-code", locObject.I },
                { "latitude", lat },
                { "longitude", lon }
            };

            var weatherProviderUrl = Configuration.WeatherProvider == WeatherProvider.Custom 
                ? Configuration.CustomWeatherProviderUrl : Configuration.GetEnumDescription(Configuration.WeatherProvider);

            weatherProviderUrl = placeholders.Aggregate(weatherProviderUrl, (current, placeholder) => current.Replace($"{{{placeholder.Key}}}", placeholder.Value));
            var isValid = Uri.TryCreate(weatherProviderUrl, UriKind.Absolute, out var uriResult)
                          && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            return isValid ? weatherProviderUrl : "";
        }
    }
}
