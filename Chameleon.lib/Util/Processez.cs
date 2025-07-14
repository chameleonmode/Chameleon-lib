using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Util;

public static class Processez {
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

	public static async Task TryKillProcess(Process? p) {
		if (p == null || p.HasExited) return;

		EX.Try(() => {
			// Attempt to close the process gracefully
			if (p.MainWindowHandle != IntPtr.Zero) _ = p.CloseMainWindow();
			// If the process is stubborn, kill it with the entire process tree.
			p.Kill(true);
			_ = p.WaitForExit(TimeSpan.FromSeconds(3)); // Wait for the process to be killed
		});

		await Task.Delay(1000);

		EX.Try(() => {
			// This is important to release the resources associated with the process.=
			// If the process has already exited, this will do nothing.
			p.Close();
			p.Dispose();
			_ = p.WaitForExit(TimeSpan.FromSeconds(1)); // Wait for the process to be killed
		});
		// Log or handle the exception if closing the process fails
		if (!p.HasExited) Toaster.Error($"Failed to close process");
	}

	public static string? GetCommandLine(Process process) {
		try {
			process.HasExited.ThrowTrue();
			if (OperatingSystem.IsWindows()) {
				using var searcher = new ManagementObjectSearcher(
					$"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
				using var objects = searcher.Get();
				foreach (var obj in objects) {
					var cmdLine = obj["CommandLine"]?.ToString();
					return cmdLine;
				}
			} else if (OperatingSystem.IsLinux()) {
				var cmdPath = $"/proc/{process.Id}/cmdline";
				if (File.Exists(cmdPath)) {
					var raw = File.ReadAllText(cmdPath);
					return raw.Replace('\0', ' ').Trim();
				}
			} else if (OperatingSystem.IsMacOS()) {
				var startInfo = new ProcessStartInfo {
					FileName = "/bin/ps",
					Arguments = $"-p {process.Id} -o command=",
					RedirectStandardOutput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				};
				using var psProc = Process.Start(startInfo);
				if (psProc != null) {
					var output = psProc.StandardOutput.ReadToEnd();
					psProc.WaitForExit();
					return output.Trim();
				}
			}
		} catch (Exception ex) {
			Debug.WriteLine($"Could not get command line for process {process.Id}: {ex.Message}");
		}
		return null;
	}

	public static T? ExtractFromCommand<T>(Process process, [StringSyntax("Regex")] string pattern, params string[] args) {
		var line = GetCommandLine(process);
		if (line.Is() || args.Any(arg => line.Contains(arg, StringComparison.OrdinalIgnoreCase))) return default;

		var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
		if (!match.Success) return default;

		var value = match.Groups[1].Value;
		return (T)(object)(
			typeof(T) == typeof(int) && int.TryParse(value, out var port)? port : value
		);
	}

	/// <summary>
	/// Check if a port is free
	/// </summary>
	/// <param name="port"></param>
	/// <returns></returns>
	public static bool IsFree(int port) {
		var properties = IPGlobalProperties.GetIPGlobalProperties();
		var listeners = properties.GetActiveTcpListeners();
		var openPorts = listeners.Select(item => item.Port).ToArray<int>();
		return openPorts.All(openPort => openPort != port);
	}

	/// <summary>
	/// Get the next free port
	/// </summary>
	/// <param name="port"></param>
	/// <returns></returns>
	public static int NextFreePort(int port = 0, int max = 99999) {
		port = (port > 0) ? port : new Random().Next(1, 65535);
		while (!IsFree(port)) {
			port += 1;
			if (port > max)
				throw new Exception("No free ports available");
		}
		return port;
	}

	/// <summary>
	/// Get a random unused port
	/// </summary>
	public static int GetRandomUnusedPort() {
		var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		var port = ((IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}
}