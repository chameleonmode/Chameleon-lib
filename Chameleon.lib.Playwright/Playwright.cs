using Chameleon.lib.Common.Constants;
using Chameleon.lib.WebBrowser.Models;
using Microsoft.Playwright;
using Chameleon.lib.Const;
using Chameleon.AIR.Scripts.Models;
using chameleon.assets;
using System.Net.NetworkInformation;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Playwright;

public class RunScriptOptions {
	public int Port { get; set; }
	public bool Record { get; set; } = false;
	public Enums.SystemBrowserType BrowserType { get; set; } = Enums.SystemBrowserType.Chromium;
	public IScript? Script { get; set; }
	public object? Opts { get; set; }
	public PlaywrightScriptDescription? Description { get; set; }
}

public record GetCookiesOptions(SysBrowserOpenOptions Browser, int? Port) {
	public Proxy? Proxy => Browser.Profile.Proxy.Server == null ? null
	 : new() {
		 Server = Browser.Profile.Proxy.Server,
		 Username = Browser.Profile.Proxy.UserName,
		 Password = Browser.Profile.Proxy.Password,
	 };

	public string Dir => Path.Combine(FilePaths.AppDataLocalDir, Browser.BrowserType.ToString(), Browser.Profile.Id.ToString());
}

public record PlaywrightScriptDescription(
	Dictionary<string, string> Parameters,
	string? Title = null,
	string? Description = null,
	string? FilePath = null
);

public static class Project {
	public static class Plugins {
		public static string DotPlaywright { get; } = Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory,
			Debug || OperatingSystem.IsWindows()
			? ".playwright"
			: "../Resources/.playwright"
		);
		public static string Dir { get; } = Path.Combine(FilePaths.AppDataDir, "playwright");
		public static string App { get; } = Path.Combine(Dir, "app.js");
		// TODO: public static string Node { get; } = Path.Combine(Playwright, "node" + (OperatingSystem.IsWindows() ? ".exe" : ""));
		public static string Node { get; } = Path.Combine(DotPlaywright, "node", OperatingSystem.IsWindows() ? "win32_x64\\node.exe" : "darwin-x64/node");
	}

	public static TaskCompletionSource<bool> Initialized { get; } = new();
	public static async Task<bool> Init() {
		var source = "plugins";
		var target = FilePaths.AppDataDir;
		var success = File.Exists(Plugins.App);
		if (!success || Debug) {
			Toaster.Info("Installing updates...");
			success = await Resources.Mapped(source, target);
			if (success) Toaster.Success("Updates installed.");
			else Toaster.Error("Failed to install updates.");
		}
		return Initialized.TrySetResult(success);
	}

	public static bool Debug { get; } =
#if DEBUG
		true;
#else
		false;
#endif
}