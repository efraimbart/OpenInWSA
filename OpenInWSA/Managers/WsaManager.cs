using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using OpenInWSA.Properties;
using SharpAdbClient;

namespace OpenInWSA.Managers
{
    public static class WsaManager
    {
        internal const string WsaClient = "WsaClient";
        
        private const string DefaultAdbLocation = @"adb";
        private const string Host = "127.0.0.1";
        private const int Port = 58526;
        
        private static readonly string HostAndPortString = $"{Host}:{Port}";
        
        private static readonly AdbServer AdbServer = new();
        private static readonly AdbClient AdbClient = new();

        internal static bool UpdateAdbLocation(bool cancelable)
        {
    
            var oldAdbLocation = Settings.Default.AdbLocation;

            var defaultValue = oldAdbLocation != null ? $" [{oldAdbLocation}]" : "";
            Console.WriteLine($@"Please enter the path to ADB:{defaultValue}");
            var adbLocation = Console.ReadLine();
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(adbLocation)) return cancelable;

            if (!TestAdbLocation(adbLocation))
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
            if (!TestAdbLocation(DefaultAdbLocation)) return false;
            
            Settings.Default.AdbLocation = DefaultAdbLocation;
            Settings.Default.Save();

            return true;

        }

        private static bool TestAdbLocation(string location)
        {
            try
            {
                AdbServer.StartServer(location, restartServerIfNewer: true);
            }
            catch
            {
                return false;
            }

            return true;
        }
        
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
                    
                    OpenInBrowser(url, "Starting WSA... opening url in browser for now.");
                    return;
                }

                if (!AdbServer.GetStatus().IsRunning)
                {
                    Console.WriteLine("Starting ADB, please wait.");
                    Console.WriteLine();
                    
                    AdbServer.StartServer(Settings.Default.AdbLocation, restartServerIfNewer: true);
                }

                var device = AdbClient.GetDevices().FirstOrDefault(device => device.Serial == HostAndPortString);
                if (device == null)
                {
                    Console.WriteLine("Connecting ADB to WSA, please wait.");
                    Console.WriteLine();

                    AdbClient.Connect(new DnsEndPoint(Host, Port));

                    while ((device = AdbClient.GetDevices().FirstOrDefault(device => device.Serial == HostAndPortString)) == null)
                    {
                        Thread.Sleep(100);
                    }
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