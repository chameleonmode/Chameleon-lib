using System.Diagnostics;

using Chameleon.lib;
using Chameleon.lib.Util;

namespace Tests.Playwright;
public abstract class PlaywrightTestsBase {
	public readonly TaskCompletionSource<bool> _tcs = new();

	public string CachePath { get; } = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
	//public string CachePath { get; } = @"C:\Users\eli\AppData\Local\Chameleon\Brave\25541";// Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

	public Process? BrowserProcess { get; set; }
	public int Port { get; set; }

	public static Process GrowserProcess(string cachepath, List<string> args) => new() {
		StartInfo = new ProcessStartInfo {
			FileName = IoC.GetValue<string>("BrowserPath"),
			Arguments = string.Join(" ", new List<string>(args) {
						"chrome://extensions/",
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

	public async Task LaunchBrowser(string? path = null) {
		Port = TcpUtil.NextFreePort(Port);
		BrowserProcess = GrowserProcess(path ?? CachePath, [$"--remote-debugging-port={Port}"]);
		_ = BrowserProcess!.Start();
		await Task.Delay(2000);
	}

	public Task DisposeBrowser() {
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
