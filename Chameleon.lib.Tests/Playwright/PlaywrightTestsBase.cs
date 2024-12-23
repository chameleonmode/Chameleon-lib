using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using Chameleon.lib.Common;
using Chameleon.lib.Common.Util;

namespace Chameleon.lib.Tests.Playwright;
public abstract class PlaywrightTestsBase {
	public readonly TaskCompletionSource<bool> _tcs = new();

	public string CachePath { get; } = @"C:\Users\eli\AppData\Local\Chameleon\Brave\25541";// Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

	public Process? BrowserProcess { get; set; }
	public int Port { get; set; }

	public static Process GrowserProcess(string cachepath, List<string> args) => new() {
		StartInfo = new ProcessStartInfo {
			FileName = IoC.GetValue<string>("BrowserPath"),
			Arguments = string.Join(" ", new List<string>(args) {
						"example.com",
						"--restore-last-session",
						"--disable-session-crashed-bubble",
						"--hide-crash-restore-bubble",
						"--profile-directory=Default",
						"--disable-domain-reliability",
						"--no-default-browser-check",
						"--no-first-run",
						"--disable-field-trial-config",
						"--disable-hyperlink-auditing",
						$"--user-data-dir=\"{cachepath}\"",
				}),
			UseShellExecute = true,
			ErrorDialog = true,
			CreateNoWindow = true,
		},
		EnableRaisingEvents = true,
	};

	public async Task LaunchBrowser()
	{
		//CachePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Port = Netil.NextFreePort(Port);
		BrowserProcess = GrowserProcess(CachePath, [$"--remote-debugging-port={Port}"]);
		_ = BrowserProcess!.Start();
		await Task.Delay(2000);
	}
	public Task DisposeBrowser()
	{
		if (BrowserProcess != null) {
			BrowserProcess.Kill();
			BrowserProcess.Dispose();
			BrowserProcess = null;
		}
		if (Directory.Exists(CachePath)) Directory.Delete(CachePath, true);
		return Task.CompletedTask;
		//await Task.Delay(2000);
		//if (Directory.Exists(CachePath)) Directory.Delete(CachePath, true);
	}
}
