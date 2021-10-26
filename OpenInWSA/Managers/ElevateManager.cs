using System;
using System.Diagnostics;
using OpenInWSA.Enums;

namespace OpenInWSA.Managers
{
    public static class ElevateManager
    {
        internal static bool Elevate(ElevateFor elevateFor)
        {
            using var currentProcess = Process.GetCurrentProcess();
            var path = currentProcess.MainModule?.FileName;

            try
            {
                    
                var startInfo = new ProcessStartInfo(path, $"/elevateFor {elevateFor}")
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
    }
}