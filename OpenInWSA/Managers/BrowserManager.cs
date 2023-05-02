using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using OpenInWSA.Classes;
using OpenInWSA.Enums;
using OpenInWSA.Properties;
using Console = OpenInWSA.Classes.Console;

namespace OpenInWSA.Managers
{
    public static class BrowserManager
    {
        internal const string OpenInWsaProgId = "OpenInWSAURL";
        
        private const string OpenInWsa = "Open In WSA";

        internal static bool UpdateDefaultBrowser(bool cancelable)
        {
            //TODO: Merge with LocalUser?
            using var registeredApplications = Registry.LocalMachine.OpenSubKey(@"Software\RegisteredApplications");
            
            //TODO: This will throw an exception if it can't find RegisteredApplications, URLAssociations or http
            var browsers = registeredApplications
                .GetValueNames()
                .Where(name => name != OpenInWsa)
                .Select(name => registeredApplications.GetValue(name) as string)
                .Where(value => value != null && value.StartsWith(@"Software\Clients\StartMenuInternet\"))
                .Select(value =>
                {
                    using var urlAssociationsKey = Registry.LocalMachine.OpenSubKey($@"{value}\URLAssociations");
                    var progId = urlAssociationsKey.GetValue("http").ToString();

                    return GetBrowserFromProgId(progId);
                }).ToList();
            
            var oldDefaultBrowser = Settings.Default.DefaultBrowser != null 
                ? new Browser(Settings.Default.DefaultBrowser)
                : null;
            var oldDefaultBrowserIndex = Settings.Default.DefaultBrowser != null && cancelable 
                ? browsers.IndexOf(oldDefaultBrowser)
                : (int?)null;

            var defaultBrowserChoice = new Choices<object>($@"Please select a default browser:")
                .AddRange(browsers, browser => browser.Name)
                .Add("Cancel", condition: cancelable)
                .Default(oldDefaultBrowserIndex)
                .Choose();

            if (defaultBrowserChoice == null)
            {
                Console.WriteLine("Bad selection");
                Console.WriteLine();
                
                return false;
            }

            switch (defaultBrowserChoice.Value)
            {
                case "Cancel":
                case Browser defaultBrowser when defaultBrowser == oldDefaultBrowser:
                    return cancelable;
                case Browser defaultBrowser:
                    Settings.Default.DefaultBrowser = defaultBrowser.ToString();
                    Settings.Default.Save();

                    Console.WriteLine(oldDefaultBrowser != null
                        ? $@"Updated the the default browser from ""{oldDefaultBrowser.Name}"" to ""{defaultBrowser.Name}"""
                        : $@"Set the the default browser to ""{defaultBrowser.Name}""");
                    Console.WriteLine();

                    return true;
                default:
                    return false;
            }
        }
        
        internal static bool RegisterAsBrowser()
        {
            Console.WriteLine(@"Registering as browser");

            if (RegisterAsBrowserInner() || ElevateManager.Elevate(ElevateFor.Register))
            {
                Console.WriteLine(@"Successfully registered as browser.");
                Console.WriteLine();

                return true;
            }
            else
            {
                Console.WriteLine(@"Failed to register as browser. To register as browser, please attempt to run the application manually as administrator.");
                Console.WriteLine();

                return false;
            }
        }

        internal static bool RegisterAsBrowserInner()
        {
            using var currentProcess = Process.GetCurrentProcess();
            var path = currentProcess.MainModule?.FileName;

            try
            {
                using var openInWsaKey = Registry.LocalMachine.CreateSubKey($@"software\Clients\StartMenuInternet\{OpenInWsa}");
                
                using var capabilitiesKey = openInWsaKey.CreateSubKey("Capabilities");
                capabilitiesKey.SetValue("ApplicationDescription", OpenInWsa);
                capabilitiesKey.SetValue("ApplicationIcon", $"\"{path}\",0");
                capabilitiesKey.SetValue("ApplicationName", OpenInWsa);

                using var urlAssociationsKey = capabilitiesKey.CreateSubKey("URLAssociations");
                urlAssociationsKey.SetValue("http", OpenInWsaProgId);
                urlAssociationsKey.SetValue("https", OpenInWsaProgId);

                using var defaultIconKey = openInWsaKey.CreateSubKey("DefaultIcon");
                defaultIconKey.SetValue("", $"\"{path}\",0");

                using var commandKey = openInWsaKey.CreateSubKey(@"shell\open\command");
                commandKey.SetValue("", $"\"{path}\"");

                using var openInWsaUrlKey = Registry.ClassesRoot.CreateSubKey(OpenInWsaProgId);
                openInWsaUrlKey.SetValue("", OpenInWsa);
                openInWsaUrlKey.SetValue("EditFlags", 0x2); //TODO: Find out if needed
                openInWsaUrlKey.SetValue("FriendlyTypeName", OpenInWsa); //TODO: Find out if needed
                openInWsaUrlKey.SetValue("", $"URL:{OpenInWsa} Protocol");
                openInWsaUrlKey.SetValue("URL Protocol", "");
                
                using var urlApplicationKey = openInWsaUrlKey.CreateSubKey("Application");
                urlApplicationKey.SetValue("ApplicationDescription", OpenInWsa);
                urlApplicationKey.SetValue("ApplicationIcon", $"\"{path}\",0");
                urlApplicationKey.SetValue("ApplicationName", OpenInWsa);

                using var urlDefaultIconKey = openInWsaUrlKey.CreateSubKey("DefaultIcon");
                defaultIconKey.SetValue("", $"\"{path}\",0");

                using var urlCommandKey = openInWsaUrlKey.CreateSubKey(@"shell\open\command");
                urlCommandKey.SetValue("", $"\"{path}\" \"%1\"");

                using var registeredApplications = Registry.LocalMachine.CreateSubKey(@"Software\RegisteredApplications");
                registeredApplications.SetValue(OpenInWsa, $@"Software\Clients\StartMenuInternet\{OpenInWsa}\Capabilities");
            }
            catch
            {
                return false;
            }

            return true;
        }

        internal static bool DeregisterAsBrowser()
        {
            Console.WriteLine(@"Deregistering as browser");

            if (DeregisterAsBrowserInner() || ElevateManager.Elevate(ElevateFor.Deregister))
            {
                Console.WriteLine(@"Successfully deregistered as browser.");
                Console.WriteLine();

                return true;
            }
            else
            {
                Console.WriteLine(@"Failed to deregister as browser. To deregister as browser, please attempt to run the application manually as administrator.");
                Console.WriteLine();

                return false;
            }
        }

        internal static bool DeregisterAsBrowserInner()
        {
            try
            {
                Registry.LocalMachine.DeleteSubKeyTree($@"software\Clients\StartMenuInternet\{OpenInWsa}", throwOnMissingSubKey: false);
                Registry.ClassesRoot.DeleteSubKeyTree(OpenInWsaProgId, throwOnMissingSubKey: false);
                
                using var registeredApplications = Registry.LocalMachine.CreateSubKey(@"Software\RegisteredApplications");
                registeredApplications.DeleteValue(OpenInWsa, throwOnMissingValue: false);
            }
            catch
            {
                return false;
            }

            return true;
        }

        internal static bool InitDefaultBrowser()
        {
            using var userChoiceKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice");
            
            //TODO: This will throw an exception if it can't find UserChoice, Progid
            var progId = userChoiceKey.GetValue("Progid").ToString();

            if (progId == OpenInWsaProgId) return false;
            
            var currentDefaultBrowser = GetBrowserFromProgId(progId);
            
            Settings.Default.DefaultBrowser = currentDefaultBrowser.ToString();
            Settings.Default.Save();

            return true;
        }

        private static Browser GetBrowserFromProgId(string progId) {
            //TODO: This will throw an exception if it can't find progId class
            using var progKey = Registry.ClassesRoot.OpenSubKey(progId);
            using var applicationNameKey = progKey.OpenSubKey("Application");
            var applicationName = applicationNameKey?.GetValue("ApplicationName").ToString() ?? progId;
            
            return new Browser
            {
                Name = applicationName,
                ProgId = progId
            };
        }

        private static bool TryGetCommandFromBrowser(Browser browser, out string command)
        {
            using var progKey = Registry.ClassesRoot.OpenSubKey(browser.ProgId);

            if (progKey == null)
            {
                command = null;
                return false;
            }
            
            //TODO: This will throw an exception if it can't find command for the progId
            using var commandKey = progKey.OpenSubKey(@"shell\open\command");
            command = commandKey.GetValue(null).ToString();

            return true;
        }


        internal static void OpenInBrowser(string url)
        {
            var browser = new Browser(Settings.Default.DefaultBrowser);

            if (!TryGetCommandFromBrowser(browser, out var command))
            {
                while (!UpdateDefaultBrowser(cancelable: false)) { }

                OpenInBrowser(url);
                return;
            }

            command = command.Replace("%1", url);
            var info = new ProcessStartInfo("cmd", $@"/c ""{command}""")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = Process.Start(info);
        }
    }
}