using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using OpenInWSA.Properties;
using SharpAdbClient;
using Console = OpenInWSA.Classes.Console;

namespace OpenInWSA.Managers
{
    public static class WsaManager
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        static extern bool PathFindOnPath([In, Out] StringBuilder pszFile, [In, Optional] string[] ppszOtherDirs);
        const int MAX_PATH = 260;
        
        internal const string WsaClient = "WsaClient";

        private const string Adb = @"adb";
        private const string ExecutableExtension = @".exe";
        private const string AdbExecutable = $@"{Adb}{ExecutableExtension}";
        private const string Host = @"127.0.0.1";
        private const int Port = 58526;
        
        private static readonly string HostAndPortString = $"{Host}:{Port}";
        
        private static readonly AdbServer AdbServer = new();
        private static readonly AdbClient AdbClient = new();

        internal static bool UpdateAdbLocation(bool cancelable)
        {
            var oldAdbLocation = Settings.Default.AdbLocation;

            var defaultValue = oldAdbLocation != null && cancelable ? $" [{oldAdbLocation}]" : "";
            Console.WriteLine($@"Please enter the path to ADB:{defaultValue}");
            var adbLocation = Console.ReadLine();
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(adbLocation) || adbLocation == oldAdbLocation) return cancelable;

            if (!TryValidateAdbLocation(adbLocation))
            {
                Console.WriteLine($@"Invalid ADB path ""{adbLocation}""");
                Console.WriteLine();
                
                return false;
            }
            
            Settings.Default.AdbLocation = adbLocation;
            Settings.Default.Save();

            Console.WriteLine(oldAdbLocation != null
                ? $@"Updated the ADB path from ""{oldAdbLocation}"" to ""{adbLocation}"""
                : $@"Set the ADB path to ""{adbLocation}""");
            Console.WriteLine();

            return true;
        }

        internal static bool InitAdbLocation()
        {
            if (!TryValidateAdbLocation(Adb)) return false;
            
            Settings.Default.AdbLocation = Adb;
            Settings.Default.Save();

            return true;

        }

        private static bool TryValidateAdbLocation(string baseLocation) => TryGetAdbLocation(baseLocation, out _);
        
        private static bool TryGetAdbLocation(string baseLocation, out string location)
        {
            location = ValidateAndGetAdbLocation(baseLocation);
            return location != null;
        }

        private static string ValidateAndGetAdbLocation(string baseLocation) => baseLocation switch
        {
            var location when File.Exists(location) => location,
            var location when Directory.Exists(location) => 
                Path.Combine(location, AdbExecutable) switch
                {
                    var locationWithAdb when File.Exists(locationWithAdb) => locationWithAdb,
                    _ => null
                },
            var location when location.EndsWith(Adb) =>
                new StringBuilder($"{location}{ExecutableExtension}", MAX_PATH) switch
                {
                    var locationWithExe when File.Exists(locationWithExe.ToString()) => locationWithExe.ToString(),
                    var locationWithExe when PathFindOnPath(locationWithExe) => locationWithExe.ToString(),
                    _ => null
                },
            var location when location.EndsWith(AdbExecutable) => 
                new StringBuilder(location, MAX_PATH) switch
                {
                    var locationSb when PathFindOnPath(locationSb) => locationSb.ToString(),
                    _ => null
                },
            _ => null
        };
        
        
        internal static void OpenInWsa(string url)
        {
            try
            {
                var proc = Process.GetProcessesByName(WsaClient).FirstOrDefault();
                if (proc == null)
                {
                    var info = new ProcessStartInfo(WsaClient)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    proc = Process.Start(info);

                    //TODO: Figure out how to reliably check if WSA is running
                    OpenInBrowser(url, "Starting WSA... opening url in browser for now.");
                    return;
                    
                    // Console.WriteLine("Starting WSA, please wait.");
                    // Console.WriteLine();
                }

                if (!AdbServer.GetStatus().IsRunning)
                {
                    Console.WriteLine("Starting ADB, please wait.");
                    Console.WriteLine();

                    if (!TryGetAdbLocation(Settings.Default.AdbLocation, out var finalAdbLocation))
                    {
                        while (!UpdateAdbLocation(cancelable: false)) {}
                        finalAdbLocation = ValidateAndGetAdbLocation(Settings.Default.AdbLocation);
                    }

                    AdbServer.StartServer(finalAdbLocation, restartServerIfNewer: true);
                }

                var device = AdbClient.GetDevices().FirstOrDefault(device => device.Serial == HostAndPortString);
                if (device == null)
                {
                    Console.WriteLine("Connecting ADB to WSA, please wait.");
                    Console.WriteLine();

                    //TODO: See if there's a way to reliably use adb connection to identify if WSA has started running
                    AdbClient.Connect(new DnsEndPoint(Host, Port));

                    do
                    {
                        // AdbClient.Connect(new DnsEndPoint(Host, Port));
                        Thread.Sleep(100);
                    } 
                    while ((device = AdbClient.GetDevices().FirstOrDefault(device => device.Serial == HostAndPortString)) == null);
                }
                
                var command = $"am start -W -a android.intent.action.VIEW -d \"{url}\"";

                AdbClient.ExecuteRemoteCommandAsync(command, device, new ConsoleOutputReceiver(),
                    new CancellationToken());
            }
            catch(Exception e)
            {
                OpenInBrowser(url, $"{e}{Environment.NewLine}{Environment.NewLine}There was an issue opening the url in WSA, opening in browser instead.");
            }
        }

        private static void OpenInBrowser(string url, string message)
        {
            Console.WriteLine(message);
            Console.WriteLine();

            BrowserManager.OpenInBrowser(url);
                
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}