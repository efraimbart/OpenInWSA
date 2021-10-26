using System;
using System.Diagnostics;
using OpenInWSA.Properties;

namespace OpenInWSA.Managers
{
    public static class WsaManager
    {
        internal static void UpdateAdbLocation()
        {
            const string defaultAdbLocation = @"adb";
    
            var oldAdbLocation = Settings.Default.AdbLocation;

            Console.WriteLine($@"Please enter the path to ADB: [{oldAdbLocation ?? defaultAdbLocation}]");
            var adbLocation = Console.ReadLine();
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(adbLocation))
            {
                if (oldAdbLocation != null)
                {
                    return;
                }
                else
                {
                    adbLocation = defaultAdbLocation;
                }
            }
    
            Settings.Default.AdbLocation = adbLocation;
            Settings.Default.Save();

            Console.WriteLine(oldAdbLocation != null
                ? $@"Updated the ADB path from ""{oldAdbLocation}"" to ""{adbLocation}"""
                : $@"Set the ADB path ""{adbLocation}""");
            Console.WriteLine();
        }
        
        internal static void OpenInWsa(string url)
        {
            var command = $"shell am start -W -a android.intent.action.VIEW -d \"{url}\"";
            var info = new ProcessStartInfo(Settings.Default.AdbLocation, command)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = Process.Start(info);
        }
    }
}