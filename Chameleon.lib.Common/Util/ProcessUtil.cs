using System.Diagnostics;
using System.Runtime.InteropServices;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Common.Util;

public static partial class ProUtil {
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

	public static async Task TryKillProcess(Process? p) {
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

	public static Process? GetChildProcess(int parentId) {
		return Process.GetProcesses().FirstOrDefault(p => {
			try {
				return p.Id != 0 && p.ParentProcessId() == parentId;
			} catch {
				return false;
			}
		});
	}
		[LibraryImport("ntdll.dll", SetLastError = true)]
	private static partial int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, uint processInformationLength, out uint returnLength);

		private struct PROCESS_BASIC_INFORMATION {
		public IntPtr ExitStatus;
		public IntPtr PebBaseAddress;
		public IntPtr AffinityMask;
		public IntPtr BasePriority;
		public IntPtr UniqueProcessId;
		public IntPtr InheritedFromUniqueProcessId;
	}

		public static int ParentProcessId(this Process process) {
		var pbi = new PROCESS_BASIC_INFORMATION();
		var status = NtQueryInformationProcess(process.Handle, 0, ref pbi, (uint)Marshal.SizeOf(pbi), out _);
		return status != 0
			? throw new Exception("NtQueryInformationProcess failed with status: " + status)
			: pbi.InheritedFromUniqueProcessId.ToInt32();
	}
}