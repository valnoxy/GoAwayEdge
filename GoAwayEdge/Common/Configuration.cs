namespace GoAwayEdge.Common
{
    internal enum SearchEngine
    {
        Google,
        Bing,
        DuckDuckGo,
        Yahoo,
        Yandex,
        Ecosia,
        Ask
    }

    internal enum EdgeChannel
    {
        Stable,
        Beta,
        Dev,
        Canary
    }

    internal class Configuration
    {
        public static EdgeChannel Channel { get; set; }
        public static SearchEngine Search { get; set; }
        public static bool Uninstall { get; set; }
    }
}
