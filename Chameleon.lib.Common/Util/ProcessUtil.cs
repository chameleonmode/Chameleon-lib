using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.Const;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Common.Util;
public static class ProUtil {
	public static void GoToUrlDefault(string Url) {
		try {
			_ = Process.Start(Url);
		} catch {
			// hack because of this: https://github.com/dotnet/corefx/issues/10361
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				_ = Process.Start(new ProcessStartInfo("cmd", $"/c start {Url.Replace("&", "^&")}") { CreateNoWindow = true });
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				_ = Process.Start("xdg-open", Url);
			} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				_ = Process.Start("open", Url);
			} else {
				throw;
			}
		}
	}

	public static async Task TryKillProcess(Process? p)
	{
		if (p != null && !p.HasExited) {
			try {
				// Attempt to close the browser gracefully
				_ = p.CloseMainWindow();

				try {
					// Wait for the process to exit
					await p.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(1500)).Token); // Wait for 1.5 seconds
					p.Close();
				} catch (TaskCanceledException) {
					// If the process hasn't, kill it
					p.Kill();
					// Wait for the process to be killed
					await p.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(1000)).Token); // Wait for 1 seconds
				}
			} catch (Exception ex) {
				if (ex.GetType() == typeof(InvalidOperationException) && ex.Message.Contains("No process is associated with this object."))
					return;
				// Log or handle the exception if closing the process fails
				Toaster.Error($"Failed to close the browser process: {ex.Message}");
			}
		}
	}

	public static Process? GetChildProcess(int parentId)
	{
		return Process.GetProcesses().FirstOrDefault(p =>
		{
			try {
				return p.Id != 0 && p.ParentProcessId() == parentId;
			} catch {
				return false;
			}
		});
	}

	public static Process Start(string fileName, string arguments, StringDictionary? env = null) {
		var p = new Process {
			StartInfo = new ProcessStartInfo {
				FileName = fileName,
				Arguments = arguments,
				UseShellExecute = false,
				ErrorDialog = true,
				CreateNoWindow = true,
				EnvironmentVariables = {
					// ["CHROME_DEPRECATED"] = "1",
					// ["CHROME_NO_SANDBOX"] = "1",
					// ["CHROME_NO_GPU"] = "1",
					// ["CHROME_DISABLE_GPU"] = "1",
					// ["CHROME_DISABLE_GPU_COMPOSITING"] = "1",
					// ["CHROME_DISABLE_OOP_FILE_HANDLING"] = "1",
					// ["CHROME_DISABLE_WEB_SECURITY"] = "1",
					// ["CHROME_DISABLE_DEV_SHM_USAGE"] = "1",
					// ["CHROME_DISABLE_GPU_RASTERIZATION"] = "1",
					// ["CHROME_DISABLE_GPU_VSYNC"] = "1",
					// ["CHROME_DISABLE_GPU_ACCELERATION"] = "1",
					// ["CHROME_DISABLE_GPU_COMPOSITING"] = "1",
					// ["CHROME_DISABLE_WEB_SECURITY"] = "1",
					// ["CHROME_DISABLE_WEB_RTC"] = "1",
					// ["CHROME_DISABLE_WEB_SECURITY"] = "1",
					// ["CHROME_DISABLE_WEB_SECURITY"] = "1",
					// ["MOZ_REMOTE_SETTINGS_DEVTOOLS"] = "1",
					// ["MOZ_DISABLE_OOP_FILE_HANDLING"] = "1",
					// ["MOZ_DISABLE_GPU_RASTERIZATION"] = "1",
					// ["MOZ_DISABLE_GPU_VSYNC"] = "1",
					// ["MOZ_DISABLE_GPU_ACCELERATION"] = "1",
					// ["MOZ_DISABLE_GPU_COMPOSITING"] = "1",
					// ["MOZ_DISABLE_WEB_SECURITY"] = "1",
					// ["MOZ_DISABLE_WEB_RTC"] = "1",
					// ["MOZ_DISABLE_WEB_SECURITY"] = "1",
					// ["MOZ_DISABLE_WEB_SECURITY"] = "1",
				},
			},
			EnableRaisingEvents = true,
		};
		_ = p.Start();
		return p;
	}


	public static bool TrySetForeground(Process? p)
	{
		if (p != null) {
#pragma warning disable CA1416 // Validate platform compatibility
			if (!OperatingSystem.IsMacOS()) {
				if (p.MainWindowHandle is nint handle && U32.IsWindow(handle)) {
					if (U32til.BringWindowToForeground(handle)) {
						return true;
					}
				}
#pragma warning restore CA1416 // Validate platform compatibility
			} else {
				if (MacOSUtil.SetForegroundWindow(p.Id)) {
					p.Refresh();
				} else {
					return true;
				}
			}
		}

		return false;
	}
}