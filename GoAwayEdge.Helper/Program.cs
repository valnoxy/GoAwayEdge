namespace GoAwayEdge.Helper
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("GoAwayEdge Helper");
            Console.WriteLine("Helper Application for intercepting the Copilot Key");
            Console.WriteLine();
            Common.Debugging.Logging.Initialize();
            var resultEnvironment = Common.Configuration.InitialEnvironment();
            if (!resultEnvironment)
                return;

            var quotedArgs = args.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg);
            var argumentJoin = string.Join(" ", quotedArgs);
            Console.WriteLine("[i] Args: " + argumentJoin);
            Console.WriteLine("[i] Opening defined external application ...");
            var p = new System.Diagnostics.Process();
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{Common.Configuration.CopilotExternalApp}\" {Common.Configuration.CopilotExternalAppArgument}",
                WorkingDirectory = Path.GetDirectoryName(Common.Configuration.CopilotExternalApp)
            };
            p.StartInfo = startInfo;
            p.Start();
            Console.ReadKey();
        }
    }
}
