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
        public static bool InitEnv()
        {
            try
            {
                // Get Ifeo-Path
                var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe\0");
                if (key == null)
                {
                    Console.WriteLine("Registry key not found.");
                    return false;
                }

                var IfeoBinaryPath = (string)key.GetValue("FilterFullPath");
                if (IfeoBinaryPath == null)
                {
                    Console.WriteLine("FilterFullPath value not found.");
                    return false;
                }

                FileConfiguration.EdgePath = Path.GetDirectoryName(IfeoBinaryPath) + "\\msedge.exe";
                FileConfiguration.IfeoPath = Path.GetDirectoryName(IfeoBinaryPath) + "\\msedge_ifeo.exe";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization failed!\n{ex.Message}", "GoAwayEdge", MessageBoxButton.OK, MessageBoxImage.Error);
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
            // Get Ifeo-Path
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe\0");
            if (key == null)
            {
                Console.WriteLine("Registry key not found.");
                return 2;
            }

            var ifeoBinaryPath = (string)key.GetValue("FilterFullPath");
            if (ifeoBinaryPath == null)
            {
                Console.WriteLine("FilterFullPath value not found.");
                return 2;
            }

            if (File.Exists(ifeoBinaryPath))
            {
                var edgeBinaryPath = Path.GetDirectoryName(ifeoBinaryPath) + "\\msedge.exe";
                ifeoBinaryPath = Path.GetDirectoryName(ifeoBinaryPath) + "\\msedge_ifeo.exe";

                var edgeHash = CalculateMD5(edgeBinaryPath);
                var ifeoHash = CalculateMD5(ifeoBinaryPath);
#if DEBUG
                if (edgeHash != ifeoHash)
                    MessageBox.Show($"The Edge Hash ({edgeHash}) and Ifeo Hash ({ifeoHash}) are not identical. Validation failed!", "GoAwayEdge", MessageBoxButton.OK, MessageBoxImage.Warning);
#endif

                return edgeHash != ifeoHash ? 1 : 0;
            }
            else
            {
                Console.WriteLine($"FilterFullPath does not exist: {ifeoBinaryPath}");
                return 2;
            }
        }

        /// <summary>
        /// Modifies the Ifeo Binary.
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
                        MessageBox.Show($"Update failed!\n{ex.Message}", "GoAwayEdge", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                    break;
                }
            }
        }

        private static string CalculateMD5(string filename)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filename);
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
