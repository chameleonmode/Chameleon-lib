using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace Chameleon.lib.Common.Extensions;
public static class ProcessExts {
	public static string? GetProcessCommandLine(this Process process) {
		return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
			? GetCommandLineWindows(process)
			: RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
			? GetCommandLineLinux(process)
			: RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? GetCommandLineMac(process) : null;
	}

	private static string? GetCommandLineWindows(Process process) {
		try {
#pragma warning disable CA1416 // Validate platform compatibility
			using var searcher = new ManagementObjectSearcher(
					$"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
			using var objects = searcher.Get();
			foreach (var obj in objects) {
				return obj["CommandLine"]?.ToString();
			}
#pragma warning restore CA1416 // Validate platform compatibility
		} catch (Exception ex) {
			Console.WriteLine($"An error occured:{ex.Message}");
		}
		return null;
	}

	private static string? GetCommandLineLinux(Process process) {
		var cmdPath = $"/proc/{process.Id}/cmdline";
		try {
			if (File.Exists(cmdPath)) {
				var raw = File.ReadAllText(cmdPath);
				return raw.Replace('\0', ' ').Trim();
			}
		} catch (Exception ex) {
			Console.WriteLine($"Failed to read {cmdPath}, error:{ex.Message}");
		}
		return null;
	}

	private static string? GetCommandLineMac(Process process) {
		try {
			var startInfo = new ProcessStartInfo {
				FileName = "/bin/ps",
				Arguments = $"-p {process.Id} -o command=",
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};
			using var psProc = Process.Start(startInfo);
			if (psProc == null)
				return null;

			var output = psProc.StandardOutput.ReadToEnd();
			psProc.WaitForExit();

			return output.Trim();
		} catch (Exception ex) {
			Console.WriteLine($"An error occured:{ex.Message}");
		}
		return null;
	}
}

public static partial class Procvoke {
	[LibraryImport("ntdll.dll", SetLastError = true)]
	private static partial int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, uint processInformationLength, out uint returnLength);

	public static int ParentProcessId(this Process process) {
		var pbi = new PROCESS_BASIC_INFORMATION();
		var status = NtQueryInformationProcess(process.Handle, 0, ref pbi, (uint)Marshal.SizeOf(pbi), out _);
		return status != 0
			? throw new Exception("NtQueryInformationProcess failed with status: " + status)
			: pbi.InheritedFromUniqueProcessId.ToInt32();
	}

	private struct PROCESS_BASIC_INFORMATION {
		public IntPtr ExitStatus;
		public IntPtr PebBaseAddress;
		public IntPtr AffinityMask;
		public IntPtr BasePriority;
		public IntPtr UniqueProcessId;
		public IntPtr InheritedFromUniqueProcessId;
	}
}

public static class ChromeProcessExtensions {
	public static void CloseAllChrome() {
		var chromeProcesses = Process.GetProcessesByName("chrome");

		foreach (var chrome in chromeProcesses) {
			try {
				chrome.Kill();
				chrome.WaitForExit();
			} catch (Exception ex) {
				Console.WriteLine($"Failed to kill process {chrome.Id}: {ex.Message}");
			}
		}
	}
}
