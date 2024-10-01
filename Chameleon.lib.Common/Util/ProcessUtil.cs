using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace Chameleon.lib.Common.Util;
public static class ProUtil {
	public static void GoToUrlDefault(string Url)
	{
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

	public static IEnumerable<Process> GetRelatedProcesses(int pid)
	{
		try {
			// Get the target process
			var targetProcess = Process.GetProcessById(pid);

			// Get all running processes
			var allProcesses = Process.GetProcesses();

			// Find related processes
			var relatedProcesses = allProcesses.Where(p => {
				try {
					// Check if the process is a child of the target process
					return p.Parent()?.Id == targetProcess.Id ||
								 // Check if the process is the parent of the target process
								 targetProcess.Parent()?.Id == p.Id ||
								 // Check if the process is a sibling (same parent) of the target process
								 (targetProcess.Parent() != null && p.Parent()?.Id == targetProcess.Parent()?.Id);
				} catch {
					// If we can't access process information, skip this process
					return false;
				}
			});

			return relatedProcesses;
		} catch (ArgumentException) {
			Console.WriteLine($"No process with ID {pid} was found.");
			return Enumerable.Empty<Process>();
		} catch (Exception ex) {
			Console.WriteLine($"An error occurred: {ex.Message}");
			return Enumerable.Empty<Process>();
		}
	}


	// Extension method to get the parent process
	private static Process? Parent(this Process process)
	{
		try {
			using var query = new ManagementObjectSearcher(
					$"SELECT ParentProcessId FROM Win32_Process WHERE ProcessId = {process.Id}");
			return query.Get().Cast<ManagementObject>()
					.Select(mo => Process.GetProcessById((int)(uint)mo["ParentProcessId"]))
					.FirstOrDefault();
		} catch {
			return null;
		}
	}

	public static Process Createa(string fileName, string arguments) =>
			new() {
				StartInfo = new ProcessStartInfo {
					FileName = fileName,
					Arguments = arguments,
					UseShellExecute = true,
					ErrorDialog = true,
					//RedirectStandardOutput = true,
					CreateNoWindow = true,
				},
				EnableRaisingEvents = true,
			};
}