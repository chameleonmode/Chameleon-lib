using System.Diagnostics;

using chameleon.assets;
using Microsoft.Playwright;
using Chameleon.lib.Helpers;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Util;
using Chameleon.lib.AIR.Scripts;

namespace Chameleon.lib.Playwright;

#region types
public enum CookieOp { Import, Export }
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
#endregion

public static class Project {
	public static class Plugins {
		public static string? Version { get => IoC.GetValue(nameof(Plugins)); set => IoC.SetValue(nameof(Plugins), value, null); }
		public static string DotPlaywright { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
			IoC.Debug || OperatingSystem.IsWindows() ? ".playwright" : "../Resources/.playwright"
		);

		public static string Dir { get; } = Path.Combine(FilePaths.AppDataDir, "playwright");
		public static string App { get; } = Staging && Path.Combine("/Users/dev/src/chameleon-playwright/dist", "app.js") is string str &&
			File.Exists(str) ? str : Path.Combine(Dir, "app.js");
		public static string Node { get; } = Path.Combine(DotPlaywright, "node", OperatingSystem.IsWindows() ? "win32_x64\\node.exe" : "darwin-x64/node");
		public static string CLI { get; } = Path.Combine(DotPlaywright, "package", "cli.js");
	}

	public static TaskCompletionSource<bool> Initialized { get; } = new();
	public static async Task<bool> Init() {
    if (Plugins.Version != IoC.Assembled) {
			Toaster.Info("Installing updates...");

			var success = await Resources.Mapped("plugins", FilePaths.AppDataDir);
			if (success) Plugins.Version = IoC.Assembled;
			else Toaster.Error("Failed to install updates.");
		}
		return Initialized.TrySetResult(true);
	}
	public static bool Staging { get; } = IoC.Debug && Debugger.IsAttached;
}