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
        Ask,
        Qwant,
        Custom
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
        public static bool UninstallEdge { get; set; }
        public static string? CustomQueryUrl { get; set; }
    }

    internal enum ModifyAction
    {
        Create,
        Update
    }

    internal class FileConfiguration
    {
        public static string EdgePath = string.Empty;
        public static string IfeoPath = string.Empty;
    }
}
