using System.Diagnostics;

using chameleon.assets;
using Microsoft.Playwright;
using Chameleon.lib.Helpers;
using Chameleon.lib.AIR.Scripts.Models;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Util;

namespace Chameleon.lib.Playwright;

public interface IBundledCSScript : IScript {
	Task Run(IBrowserContext browserContext, IDictionary<string, string>? options = null);
}
public interface IExternalScript {
	Task Run(IBrowserContext browserContext, IDictionary<string, string>? pargs = null);
}
public interface IPlaywrightBrowserInstance : IDisposable {
	IBrowserContext BrowserContext { get; }
}

public interface IPlaywrightBrowser : IDisposable {
	IList<IPlaywrightBrowserInstance> RunningAutomationBrowsers { get; }
	Task<IPlaywrightBrowserInstance> Open(Arguments options);
}
public static class Project {
	public static class Plugins {
		public static string DotPlaywright { get; } = Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory,
			Debug || OperatingSystem.IsWindows()
			? ".playwright"
			: "../Resources/.playwright"
		);
		public static string Dir { get; } = Path.Combine(FilePaths.AppDataDir, "playwright");
		public static string App { get; } = Staging
		? Path.Combine("/Users/dev/src/chameleon-playwright/dist", "app.js")
		: Path.Combine(Dir, "app.js");
		public static string Node { get; } = Path.Combine(DotPlaywright, "node", OperatingSystem.IsWindows() ? "win32_x64\\node.exe" : "darwin-x64/node");
		// TODO: public static string Node { get; } = Path.Combine(Playwright, "node" + (OperatingSystem.IsWindows() ? ".exe" : ""));
	}

	public static TaskCompletionSource<bool> Initialized { get; } = new();
	public static async Task<bool> Init() {
		var source = "plugins";
		var target = FilePaths.AppDataDir;
		var success = File.Exists(Plugins.App);
		if (!Staging && (!success || Debug)) {
			Toaster.Info("Installing updates...");
			success = await Resources.Mapped(source, target);
			if (success) Toaster.Success("Updates installed.");
			else Toaster.Error("Failed to install updates.");
		}
		return Initialized.TrySetResult(success);
	}
	
	public static bool Staging { get; } = true && Debugger.IsAttached;
	public static bool Debug { get; } =
#if DEBUG
		true;
#else
		false;
#endif
}