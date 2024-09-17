using System.Diagnostics;

using Chameleon.lib.Common;
using Chameleon.lib.Common.Util;

namespace Chameleon.lib.Tests.Playwright;
public abstract class PlaywrightTestsBase {
	public readonly TaskCompletionSource<bool> _tcs = new();

	public string? CachePath;
	public Process? BrowserProcess;
	public int Port = 9669;
	public PlaywrightTestsBase()
	{
		// Setup code
		Port = Netil.NextFreePort(Port);
		CachePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		BrowserProcess = GrowserProcess(CachePath, [$"--remote-debugging-port={Port}"]);
	}

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
		_ = BrowserProcess!.Start();
		await Task.Delay(2000);
	}
	public async Task DisposeBrowser()
	{
		if (BrowserProcess != null) {
			BrowserProcess.Kill();
			BrowserProcess.Dispose();
		}
		await Task.Delay(2000);
		if (Directory.Exists(CachePath)) Directory.Delete(CachePath, true);
	}
}
