namespace GoAwayEdge.Common.Debugging
{
    internal class DebugMessage
    {
        /// <summary>
        /// Displays a debug message if the application is in debug mode.
        /// </summary>
        /// <param name="title">The title of the debug message.</param>
        /// <param name="message">The content of the debug message.</param>
        public static void DisplayDebugMessage(string title, string message)
        {
            if (!App.IsDebug) return;
            var w = new MessageUi(title, message, "OK", isMainThread: true);
            w.ShowDialog();
        }
    }
}
