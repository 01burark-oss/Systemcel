using System;
using System.Diagnostics;
using System.IO;

namespace CashTracker.App.Services
{
    internal static class InstallerLaunchService
    {
        public static bool TryScheduleInstall(string installerPath, int waitForProcessId)
        {
            if (string.IsNullOrWhiteSpace(installerPath) || !File.Exists(installerPath))
                return false;

            var escapedInstaller = installerPath.Replace("'", "''");
            var installedExe = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "CashTracker",
                "CashTracker.exe");
            var escapedInstalledExe = installedExe.Replace("'", "''");
            var script =
                $"$installer='{escapedInstaller}'; " +
                $"$installedExe='{escapedInstalledExe}'; " +
                $"$pidToWait={waitForProcessId}; " +
                "if($pidToWait -gt 0){ try{ Wait-Process -Id $pidToWait -Timeout 300 -ErrorAction Stop } catch{ } } " +
                "$process = Start-Process -FilePath $installer -ArgumentList '/CURRENTUSER /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS=desktopicon' -WindowStyle Hidden -PassThru; " +
                "if($null -ne $process){ try{ $process.WaitForExit() } catch{ } } " +
                "if(Test-Path $installedExe){ Start-Sleep -Milliseconds 500; Start-Process -FilePath $installedExe -WorkingDirectory (Split-Path -Parent $installedExe) }";

            try
            {
                var psi = new ProcessStartInfo("powershell")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                psi.ArgumentList.Add("-NoProfile");
                psi.ArgumentList.Add("-ExecutionPolicy");
                psi.ArgumentList.Add("Bypass");
                psi.ArgumentList.Add("-Command");
                psi.ArgumentList.Add(script);
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
