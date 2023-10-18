using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using System.Windows;

namespace GoAwayEdge.Common
{
    internal class Updater
    {
        /// <summary>
        /// Initialize the current environment.
        /// </summary>
        /// <returns>
        ///     Boolean status of the initialization.
        /// </returns>
        public static bool InitialEnvironment()
        {
            try
            {
                var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe\0");
                if (key == null)
                {
                    Console.WriteLine("Registry key not found.");
                    return false;
                }

                var ifeoBinaryPath = (string)key.GetValue("FilterFullPath")!;
                if (string.IsNullOrEmpty(ifeoBinaryPath))
                {
                    Console.WriteLine("FilterFullPath value not found.");
                    return false;
                }

                FileConfiguration.EdgePath = Path.Combine(Path.GetDirectoryName(ifeoBinaryPath)!, "msedge.exe");
                FileConfiguration.IfeoPath = Path.Combine(Path.GetDirectoryName(ifeoBinaryPath)!, "msedge_ifeo.exe");
                return true;
            }
            catch (Exception ex)
            {
                var messageUi = new MessageUI("GoAwayEdge",
                    $"Initialization failed!\n{ex.Message}", "OK", null, true);
                messageUi.ShowDialog();
                return false;
            }
        }

        /// <summary>
        /// Validates if the installed IFEO-Binary is identical with the Edge-Binary.
        /// </summary>
        /// <returns>
        ///     Integer value if the Binary is identical, not identical or missing.
        ///     0 : true
        ///     1 : false
        ///     2 : missing
        /// </returns>
        public static int ValidateIfeoBinary()
        {
            var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe\0");
            if (key == null)
            {
                Console.WriteLine("Registry key not found.");
                return 2;
            }

            var binaryPath = (string)key.GetValue("FilterFullPath")!;
            if (string.IsNullOrEmpty(binaryPath))
            {
                Console.WriteLine("FilterFullPath value not found.");
                return 2;
            }

            var edgeBinaryPath = Path.Combine(Path.GetDirectoryName(binaryPath)!, "msedge.exe");
            var ifeoBinaryPath = Path.Combine(Path.GetDirectoryName(binaryPath)!, "msedge_ifeo.exe");

            if (File.Exists(ifeoBinaryPath))
            {
                var edgeHash = CalculateMd5(edgeBinaryPath);
                var ifeoHash = CalculateMd5(ifeoBinaryPath);
#if DEBUG
                if (edgeHash != ifeoHash) {
                    var messageUi = new MessageUI("GoAwayEdge",
                        $"The Edge Hash ({edgeHash}) and Ifeo Hash ({ifeoHash}) are not identical. Validation failed!", "OK", null,true);
                    messageUi.ShowDialog();
                }
#endif
                return edgeHash != ifeoHash ? 1 : 0;
            }

            Console.WriteLine($"Ifeo binary does not exist: {ifeoBinaryPath}");
            return 2;
        }

        /// <summary>
        /// Modifies the Ifeo binary.
        /// </summary>
        public static void ModifyIfeoBinary(ModifyAction action)
        {
            switch (action)
            {
                case ModifyAction.Update:
                case ModifyAction.Create:
                {
                    try
                    {
                        File.Copy(FileConfiguration.EdgePath, FileConfiguration.IfeoPath, true);
                        new ToastContentBuilder()
                            .AddText("Update successful")
                            .AddText("The IFEO binary was successfully updated.")
                            .Show();
                    }
                    catch (Exception ex)
                    {
                        var messageUi = new MessageUI("GoAwayEdge",
                            $"Update failed!\n{ex.Message}!", "OK", null, true);
                        messageUi.ShowDialog();
                    }
                    break;
                }
            }
        }

        private static string CalculateMd5(string filename)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filename);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
