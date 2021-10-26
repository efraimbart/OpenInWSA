using System;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Microsoft.Win32;
using OpenInWSA.Classes;
using OpenInWSA.Enums;
using OpenInWSA.Properties;

const string openInWsa = "OpenInWSA";
const string openInWsaProgId = "OpenInWSAURL";

CheckElevate();
CheckInit();

var urlValidity = TryGetUrl(out var url);

switch (urlValidity)
{
    case UrlValidity.InvalidUrl:
        if (args.Any())
        {
            Console.WriteLine(@$"Invalid url '{args[0]}'");
            Console.WriteLine();
        }
            
        while (MainMenu()) { }
        break;
    case UrlValidity.OpenInWsa:
        OpenInWsa(url);
        break;
    case UrlValidity.OpenInBrowser:
        OpenInBrowser(url);
        break;
}

void CheckElevate()
{
    if (args.Any() && args[0] == "/elevate")
    {
        var exitCode = RegisterAsBrowserInner() ? 0 : 1;
        Environment.Exit(exitCode);
    }
}

void CheckInit()
{
    if (Settings.Default.AdbLocation == null)
    {
        UpdateAdbLocation();
    }

    if (Settings.Default.DefaultBrowser == null)
    {
        if (!InitDefaultBrowser())
        {
            while (!UpdateDefaultBrowser(cancelable: false)) {}
        }
    }

    if (!Settings.Default.RegisteredAsBrowser)
    {
        if (RegisterAsBrowser())
        {
            Settings.Default.RegisteredAsBrowser = true;
            Settings.Default.Save();
        }
        else
        {
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            Environment.Exit(1);
        }
    }
}

bool RegisterAsBrowser()
{
    Console.WriteLine(@"Registering as browser");

    if (RegisterAsBrowserInner() || Elevate())
    {
        Console.WriteLine(@"Successfully registered as browser.");
        Console.WriteLine();

        return true;
    }
    else
    {
        Console.WriteLine(@"Failed to register as browser. To register as browser, please attempt to run the application manually as administrator.");

        return false;
    }
}

bool RegisterAsBrowserInner()
{
    using var currentProcess = Process.GetCurrentProcess();
    var path = currentProcess.MainModule?.FileName;

    try
    {
        using var openInWsaKey = Registry.LocalMachine.CreateSubKey($@"software\Clients\StartMenuInternet\{openInWsa}");
        
        using var capabilitiesKey = openInWsaKey.CreateSubKey("Capabilities");
        capabilitiesKey.SetValue("ApplicationDescription", "Open In WSA");
        capabilitiesKey.SetValue("ApplicationIcon", $"\"{path}\",0");
        capabilitiesKey.SetValue("ApplicationName", openInWsa);

        using var urlAssociationsKey = capabilitiesKey.CreateSubKey("URLAssociations");
        urlAssociationsKey.SetValue("http", openInWsaProgId);
        urlAssociationsKey.SetValue("https", openInWsaProgId);

        using var defaultIconKey = openInWsaKey.CreateSubKey("DefaultIcon");
        defaultIconKey.SetValue("", $"\"{path}\",0");

        using var commandKey = openInWsaKey.CreateSubKey(@"shell\open\command");
        commandKey.SetValue("", $"\"{path}\"");

        using var openInWsaUrlKey = Registry.ClassesRoot.CreateSubKey(openInWsaProgId);
        openInWsaUrlKey.SetValue("", openInWsa);
        openInWsaUrlKey.SetValue("EditFlags", 0x2); //TODO: Find out if needed
        openInWsaUrlKey.SetValue("FriendlyTypeName", openInWsa); //TODO: Find out if needed
        openInWsaUrlKey.SetValue("URL Protocol", "");

        using var urlDefaultIconKey = openInWsaUrlKey.CreateSubKey("DefaultIcon");
        defaultIconKey.SetValue("", $"\"{path}\",0");
        
        using var urlCommandKey = openInWsaUrlKey.CreateSubKey(@"shell\open\command");
        urlCommandKey.SetValue("", $"\"{path}\" \"%1\"");

        using var registeredApplications = Registry.LocalMachine.CreateSubKey(@"Software\RegisteredApplications");
        registeredApplications.SetValue(openInWsa, @"Software\Clients\StartMenuInternet\OpenInWSA\Capabilities");
    }
    catch
    {
        return false;
    }

    return true;
}

UrlValidity TryGetUrl(out Uri url)
{
    if (!args.Any())
    {
        url = null;
        return UrlValidity.InvalidUrl;
    }
            
    var urlString = args[0];

    if (string.IsNullOrWhiteSpace(urlString) ||
        !Uri.TryCreate(urlString, UriKind.Absolute, out var tempUrl))
    {
        url = null;
        return UrlValidity.InvalidUrl;
    }
    
    var query = HttpUtility.ParseQueryString(tempUrl.Query);

    if (query.GetValues(openInWsa)?.Any() ?? false)
    {
        query.Remove(openInWsa);

        var uriBuilder = new UriBuilder(tempUrl)
        {
            Query = query.ToString()
        };

        url = uriBuilder.Uri;
        return UrlValidity.OpenInBrowser;
    }
    else
    {
        query.Set(openInWsa, true.ToString());

        var uriBuilder = new UriBuilder(tempUrl)
        {
            Query = query.ToString()
        };

        url = uriBuilder.Uri;
        return UrlValidity.OpenInWsa;
    }
}

bool MainMenu()
{
    var mainMenuChoice = 
        new Choices<MainMenuChoices>(@"What would you like to do?")
            .Add(@"Update ADB location", MainMenuChoices.AdbLocation)
            .Add(@"Update default browser", MainMenuChoices.DefaultBrowser)
            .Add(@"Re-register as browser", MainMenuChoices.ReRegisterAsBrowser)
            .Add("Exit", MainMenuChoices.Exit)
            .Choose();
                
    switch (mainMenuChoice?.Value)
    {
        case MainMenuChoices.AdbLocation:
            UpdateAdbLocation();
            break;
        case MainMenuChoices.DefaultBrowser:
            UpdateDefaultBrowser(cancelable: true);
            break;
        case MainMenuChoices.ReRegisterAsBrowser:
            return RegisterAsBrowser();
        case MainMenuChoices.Exit:
            return false;
    }

    return true;
}

void UpdateAdbLocation()
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

bool UpdateDefaultBrowser(bool cancelable)
{
    //TODO: Merge with LocalUser?
    using var registeredApplications = Registry.LocalMachine.OpenSubKey(@"Software\RegisteredApplications");
    
    //TODO: This will throw an exception if it can't find RegisteredApplications, URLAssociations or http
    var browsers = registeredApplications
        .GetValueNames()
        .Select(name => registeredApplications.GetValue(name) as string)
        .Where(value => value != null && value.StartsWith(@"Software\Clients\StartMenuInternet\"))
        .Select(value =>
        {
            using var urlAssociationsKey = Registry.LocalMachine.OpenSubKey($@"{value}\URLAssociations");
            var progId = urlAssociationsKey.GetValue("http").ToString();
            
            return GetBrowserFromProgId(progId);
        }).ToList();

    var browserChoices = browsers.Select(browser => browser.Name).ToList();
    
    var oldDefaultBrowser = Settings.Default.DefaultBrowser;
    var oldDefaultBrowserIndex = oldDefaultBrowser != null
        ? (int?) browserChoices.IndexOf(new Browser(oldDefaultBrowser).Name)
        : null;

    var defaultBrowserChoice = new Choices($@"Please select a default browser:", browserChoices)
        .Add("Cancel", condition: cancelable)
        .Default(oldDefaultBrowserIndex)
        .Choose();

    if (defaultBrowserChoice == null) return false;

    switch (defaultBrowserChoice.Value)
    {
        case "Cancel":
            return false;
        default:
            var defaultBrowser = browsers.First(browser => browser.Name == defaultBrowserChoice.Value);
            
            Settings.Default.DefaultBrowser = defaultBrowser.ToString();
            Settings.Default.Save();

            Console.WriteLine(oldDefaultBrowser != null
                ? $@"Updated the the default browser from ""{new Browser(oldDefaultBrowser).Name}"" to ""{defaultBrowser.Name}"""
                : $@"Set the the default browser to ""{defaultBrowser.Name}""");
            Console.WriteLine();

            return true;
    }
}

bool InitDefaultBrowser()
{
    using var userChoiceKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice");
    
    //TODO: This will throw an exception if it can't find UserChoice, Progid
    var progId = userChoiceKey.GetValue("Progid").ToString();

    if (progId == openInWsaProgId) return false;
    
    var currentDefaultBrowser = GetBrowserFromProgId(progId);
    
    Settings.Default.DefaultBrowser = currentDefaultBrowser.ToString();
    Settings.Default.Save();

    return true;
}

Browser GetBrowserFromProgId(string progId) {
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

string GetCommandFromBrowser(Browser browser)
{
    //TODO: This will throw an exception if it can't find command
    using var progKey = Registry.ClassesRoot.OpenSubKey(browser.ProgId);
    using var commandKey = progKey.OpenSubKey(@"shell\open\command");
    var command = commandKey.GetValue(null).ToString();

    return command;
}

void OpenInWsa(Uri url)
{
    var command = $"shell am start -W -a android.intent.action.VIEW -d \"{url}\"";
    var info = new ProcessStartInfo(Settings.Default.AdbLocation, command)
    {
        UseShellExecute = false,
        CreateNoWindow = true
    };
    var proc = Process.Start(info);
}

void OpenInBrowser(Uri url)
{
    var browser = new Browser(Settings.Default.DefaultBrowser);

    var command = GetCommandFromBrowser(browser).Replace("%1", url.ToString());
    var info = new ProcessStartInfo("cmd", $"/c {command}")
    {
        UseShellExecute = false,
        CreateNoWindow = true
    };
    var proc = Process.Start(info); 
}

bool Elevate()
{
    using var currentProcess = Process.GetCurrentProcess();
    var path = currentProcess.MainModule?.FileName;

    try
    {
                    
        var startInfo = new ProcessStartInfo(path, "/elevate")
        {
            UseShellExecute = true,
            Verb = "runas"
        };

        var process = Process.Start(startInfo);

        if (process == null) return false;

        process.WaitForExit();

        return process.ExitCode == 0;
    }
    catch
    {
        return false;
    }
}