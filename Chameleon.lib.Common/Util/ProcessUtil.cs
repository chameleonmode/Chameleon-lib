using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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