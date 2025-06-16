using System.Diagnostics;
using System.Runtime.InteropServices;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Util;

public static class ProcessUtil {
	public static void OpenBrowser(string Url) {
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

	/// <summary>
	/// Opens the specified folder in the system file explorer.
	/// </summary>
	/// <param name="folderPath">The full path to the folder to open.</param>
	/// <exception cref="PlatformNotSupportedException">Thrown if the current OS is not Windows or macOS.</exception>
	public static void OpenFolder(string folderPath) {
		if (string.IsNullOrWhiteSpace(folderPath))
			throw new ArgumentException("Folder path cannot be null or whitespace.", nameof(folderPath));

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			// For Windows, use Explorer.exe.
			_ = Process.Start(new ProcessStartInfo {
				FileName = "explorer.exe",
				Arguments = $"\"{folderPath}\"", // Enclose in quotes to handle spaces in the path.
				UseShellExecute = true
			});
		} else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
			// For macOS, use the 'open' command.
			_ = Process.Start("open", folderPath);
		} else {
			throw new PlatformNotSupportedException("This platform is not supported.");
		}
	}

	public static async Task TryKillProcess(Process? p) {
		if (p != null && !p.HasExited) {
			try {
				// Attempt to close the browser gracefully
				_ = p.CloseMainWindow();

				try {
					// Wait for the process to exit
					await p.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(3000)).Token); // Wait for 3 seconds
					p.Close();
				} catch (TaskCanceledException) {
					// If the process hasn't, kill it
					p.Kill();
					// Wait for the process to be killed
					await p.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromMilliseconds(1000)).Token); // Wait for 1 seconds
				}
			} catch (Exception ex) {
				if (ex.GetType() == typeof(InvalidOperationException) && ex.Message.Contains("No process is associated with this object.")) return;
				// Log or handle the exception if closing the process fails
				Toaster.Error($"Failed to close: {ex.Message}");
			}
		}
	}
}