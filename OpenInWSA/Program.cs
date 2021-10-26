using System;
using System.Management;
using System.Diagnostics;
using System.Linq;
using OpenInWSA.Classes;
using OpenInWSA.Enums;
using OpenInWSA.Extensions;
using OpenInWSA.Managers;
using OpenInWSA.Properties;

CheckElevate();
CheckInit();

if (args.Any())
{
    if (GetParentProcess().ProcessName == WsaManager.WsaClient)
    {
        BrowserManager.OpenInBrowser(args[0]);
    }
    else
    {
        WsaManager.OpenInWsa(args[0]);
    }
}
else
{
    while (MainMenu()) { }
}

void CheckElevate()
{
    if (args.Any() && args[0] == "/elevateFor")
    {

        if (!Enum.TryParse<ElevateFor>(args[1], out var elevateFor))
        {
            Environment.Exit(1);
        }
                
        int exitCode;
        switch (elevateFor)
        {
            case ElevateFor.Register:
                exitCode = BrowserManager.RegisterAsBrowserInner() ? 0 : 1;
                Environment.Exit(exitCode);
                break;
            case ElevateFor.Deregister:
                exitCode = BrowserManager.DeregisterAsBrowserInner() ? 0 : 1;;
                Environment.Exit(exitCode);
                break;
        }
    }
}

void CheckInit()
{
    if (Settings.Default.AdbLocation == null)
    {
        if (!WsaManager.InitAdbLocation())
        {
            while (!WsaManager.UpdateAdbLocation(cancelable: false)) {}
        }
    }

    if (Settings.Default.DefaultBrowser == null)
    {
        if (!BrowserManager.InitDefaultBrowser())
        {
            while (!BrowserManager.UpdateDefaultBrowser(cancelable: false)) {}
        }
    }

    if (!Settings.Default.RegisteredAsBrowser)
    {
        if (BrowserManager.RegisterAsBrowser())
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

Process GetParentProcess()
{
    //https://stackoverflow.com/questions/2531837/how-can-i-get-the-pid-of-the-parent-process-of-my-application/2533287#2533287
    var myId = Environment.ProcessId;
    var query = $"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {myId}";
    var search = new ManagementObjectSearcher("root\\CIMV2", query);
    var queryObj = search.Get().First();
    var parentId = (uint)queryObj["ParentProcessId"];
    var parent = Process.GetProcessById((int)parentId);

    return parent;
}

bool MainMenu()
{
    var mainMenuChoice = 
        new Choices<MainMenuChoices>(@"What would you like to do?")
            .Add(@"Update ADB location", MainMenuChoices.AdbLocation)
            .Add(@"Update default browser", MainMenuChoices.DefaultBrowser)
            .Add(@"Re-register as browser", MainMenuChoices.ReRegisterAsBrowser)
            .Add(@"Deregister as browser", MainMenuChoices.DeregisterAsBrowser)
            .Add("Exit", MainMenuChoices.Exit)
            .Choose();
                
    switch (mainMenuChoice?.Value)
    {
        case MainMenuChoices.AdbLocation:
            while (!WsaManager.UpdateAdbLocation(cancelable: true)) {}
            break;
        case MainMenuChoices.DefaultBrowser:
            while (!BrowserManager.UpdateDefaultBrowser(cancelable: true)) {}
            break;
        case MainMenuChoices.ReRegisterAsBrowser:
            BrowserManager.RegisterAsBrowser();
            break;
        case MainMenuChoices.DeregisterAsBrowser:
            BrowserManager.DeregisterAsBrowser();
            break;
        case MainMenuChoices.Exit:
            return false;
    }

    return true;
}
