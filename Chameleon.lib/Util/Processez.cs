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
		EX.Try(() => {
			if (p == null || p.HasExited) return;
			// Attempt to close the process gracefully
			_ = p.CloseMainWindow();
			_ = p.WaitForExit(TimeSpan.FromSeconds(1)); // Wait for the process to be killed
		});
		await Task.Delay(600);
		EX.Try(() => {
			p?.Kill(true);
			p?.Close();
			p?.Dispose();
		});
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

	public static T? ExtractArgs<T>(this Process process, [StringSyntax("Regex")] string pattern, params (string regex, string value)[] args) {
		var cmd = GetCommandLine(process);
		if (!MatchesArgs(cmd, args)) return default;

		var match = Regex.Match(cmd, pattern, RegexOptions.IgnoreCase);
		if (!match.Success) return default;

		var val = match.Groups[1].Value;
		return typeof(T) == typeof(int?) && int.TryParse(val, out var number) ? (T)(object)number : (T)(object)val;
	}

	public static bool ExtractArgs(this Process process, params (string regex, string value)[] args) {
		return MatchesArgs(GetCommandLine(process), args);
	}

	private static bool MatchesArgs([NotNullWhen(true)] string? cmd, (string regex, string value)[] args) {
		return cmd.IsNot() && args.Any(arg => {
			var match = Regex.Match(cmd, arg.regex, RegexOptions.IgnoreCase);
			Debug.WriteLine($"Checking argument '{arg.regex}' in command line: {cmd}");
			return match.Success && match.Groups.Values.Any(v => v.Value.StartsWith(arg.value, StringComparison.OrdinalIgnoreCase));
		});
	}

	/// <summary>
	/// Check if a port is free
	/// </summary>
	/// <param name="port"></param>
	/// <returns></returns>
	public static bool IsFree(int port) {
		// var properties = IPGlobalProperties.GetIPGlobalProperties();
		// var listeners = properties.GetActiveTcpListeners();
		// var openPorts = listeners.Select(item => item.Port).ToArray<int>();
		// return openPorts.All(openPort => openPort != port);
		try {
			using var listener = new TcpListener(IPAddress.Loopback, port);
			listener.Start();
			listener.Stop();
			return true;
		} catch (SocketException) {
			return false;
		}
	}

	/// <summary>
	/// Get the next free port
	/// </summary>
	/// <param name="port"></param>
	/// <returns></returns>
	public static int NextFreePort(int start, int max = 65535) {
		var port = (start > 0) ? start : new Random().Next(1, 65535);
		while (!IsFree(port)) {
			if (++port > max) port = start;
		}
		return port;
	}
}