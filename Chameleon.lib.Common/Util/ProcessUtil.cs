using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

using Chameleon.lib.Common.Extensions;
using Chameleon.lib.Common.Util.Mac;
using Chameleon.lib.Common.Util.Win;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Common.Util;
public static class ProUtil {
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

	public static Process Createa(string fileName, string arguments) =>
			new() {
				StartInfo = new ProcessStartInfo {
					FileName = fileName,
					Arguments = arguments,
					UseShellExecute = false,
					ErrorDialog = true,
					CreateNoWindow = true,
				},
				EnableRaisingEvents = true,
			};

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