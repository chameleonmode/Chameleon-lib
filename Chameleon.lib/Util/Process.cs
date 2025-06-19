using System.Diagnostics;
using System.Linq.Expressions;
using System.Management;
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
		if (p == null || p.HasExited) return;
		try {
			// Attempt to close the browser gracefully
			if (p.MainWindowHandle != IntPtr.Zero) _ = p.CloseMainWindow();
			// If the process hasn't, kill it
			// If the process is stubborn, kill it with the entire process tree on Windows.
			if (OperatingSystem.IsWindows()) p.Kill(true);
			else p.Kill();
			// Wait for the process to be killed
			await p.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token); // Wait for 1 seconds
		} catch { }

		try {
			// Close the process handle
			// This is important to release the resources associated with the process.
			// This is a best effort to close the process gracefully.
			// If the process has already exited, this will do nothing.
			p.Close();
			p.Dispose();
			await p.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token); // Wait for 1 seconds
		} catch (Exception ex) {
			if (ex.GetType() == typeof(InvalidOperationException) && ex.Message.Contains("No process is associated with this object.")) return;
		}
		// Log or handle the exception if closing the process fails
		if(!p.HasExited) Toaster.Error($"Failed to close process");
	}
	//??
	// public static async Task TryKillProcess(Process? p) {
	// 	if (p == null || p.HasExited) {
	// 		return;
	// 	}

	// 	try {
	// 		var hasWindow = false;
	// 		try {
	// 			hasWindow = !p.HasExited && p.MainWindowHandle != IntPtr.Zero;
	// 		} catch (InvalidOperationException) {
	// 			Debug.WriteLine("The process has already exited");
	// 		}

	// 		if (hasWindow) {
	// 			// Attempt to gracefully close the window
	// 			_ = p.CloseMainWindow();
	// 			try {
	// 				// Wait for 3 seconds for the process to exit.
	// 				await p.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token);
	// 			} catch (TaskCanceledException) {
	// 				Debug.WriteLine("The process did not close in time, it will be killed");
	// 			}
	// 		}

	// 		// This handles headless processes, and processes that failed to close gracefully
	// 		if (!p.HasExited) {
	// 			try {
	// 				if (OperatingSystem.IsWindows()) {
	// 					p.Kill(true); // Kill the entire process tree on Windows.
	// 				} else {
	// 					p.Kill();
	// 				}
	// 				// Wait for 1 second for the process to be killed.
	// 				await p.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
	// 			} catch (TaskCanceledException) {
	// 				Debug.WriteLine("The process is stubborn");
	// 			}
	// 		}
	// 	} catch (Exception ex) {
	// 		if (ex is InvalidOperationException && ex.Message.Contains("No process is associated with this object.")) {
	// 			Debug.WriteLine("The process terminated unexpectedly");
	// 			return;
	// 		}
	// 		Toaster.Error($"Failed to terminate process {p.Id}: {ex.Message}");
	// 	} finally {
	// 		p.Close();
	// 	}
	// }

	public static string GetCommandLine(Process process) {
		if (OperatingSystem.IsWindows()) {
			try {
				using var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id);
				using var objects = searcher.Get();
				var commandLine = objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
				return commandLine ?? "";
			} catch (Exception ex) {
				Debug.WriteLine($"Could not get command line for process {process.Id} on Windows: {ex.Message}");
				return "";
			}
		} else if (OperatingSystem.IsLinux()) {
			try {
				return File.ReadAllText($"/proc/{process.Id}/cmdline").Replace('\0', ' ');
			} catch (Exception ex) {
				Debug.WriteLine($"Could not get command line for process {process.Id} on Linux: {ex.Message}");
				return "";
			}
		} else if (OperatingSystem.IsMacOS()) {
			Debug.WriteLine($"GetCommandLine is not implemented for macOS for process {process.Id}.");
			return "";
		}
		return "";
	}

	public static bool HasCommandLineArgument(Process process, string argument)
	{
		if (process == null || process.HasExited)
		{
			return false;
		}

		var commandLine = GetCommandLine(process);
		if (string.IsNullOrEmpty(commandLine))
		{
			return false;
		}

		return commandLine.Contains(argument, StringComparison.OrdinalIgnoreCase);
	}
}