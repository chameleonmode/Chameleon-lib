using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using chameleon.assets;
using Chameleon.lib.Browzio.Services.Browzas;
using Chameleon.lib.Helpers;
using Chameleon.lib.Services;
using Chameleon.lib.Util;
using Chameleon.lib.Playwright;
using Chameleon.lib.Browzio.Services;

namespace Chameleon.lib.Browzio;

#region types
public enum BrowserType {
	Unknown,
	Chrome, Edge, Brave, Opera, Vivaldi, Chromium,
	Firefox, Waterfox, LibreWolf,
	Safari, Yandex, Arc, InternetExplorer
}
public enum BrowserEngine { Unknown, Chromium, Gecko, WebKit, Other }

public record BrowserInfo(BrowserType Type, string ExecutablePath, string Version, BrowserEngine Engine) {
	public string Name => Type.ToString();
	public string DisplayName => !string.IsNullOrEmpty(Version) ? $"{Name} {Version}" : Name;
	public string? IconData { get; } = null;//IconExtractor.ExtractIcon(ExecutablePath);
	public string ExecutableName => Path.GetFileName(ExecutablePath).Replace(".exe", "", StringComparison.OrdinalIgnoreCase);
}
public class BrowserProxy(string? host = null, int port = 0, string? userName = null, string? password = null) {
	public NetworkCredential? Credentials => userName.Is() || password.Is() ? null : new(userName, password);
	public WebProxy? WebProxy => host.Is() || port <= 0
		? null
		: new WebProxy($"{host}:{port}") { Credentials = Credentials };
}
public class BrowserProfile(string? url = null) {
	public int Id { get; init; } = -1; // -1 is a special value for the default profile
	public BrowserProxy Proxy { get; set; } = new();

	public EmulationOptions Emulations { get; init; } = IoC.GetJsonValue<EmulationOptions>(nameof(EmulationOptions)) ?? new();
	public string[] Bookmarks { get; init; } = IoC.GetJsonValue<string[]>(nameof(Bookmarks)) ?? [];

	public string StartPage => url ?? IoC.GetValue(nameof(StartPage))
		.Let(l => l.Is()
			? "about:blank"
			: Uri.TryCreate(l, UriKind.Absolute, out var uriResult)
				? uriResult.AbsoluteUri
				: "http://" + l);
}
public record BrowserSetting(BrowserType BrowserType, BrowserProfile Profile) {
	public required int Port { get; set; }
	public (string host, int port)? Proxio { get; set; }
	public bool WithExtensions => Profile.Id > 0;
	public string CachePath => FilePaths.EnsureDirectoryExists(
		FilePaths.AppDataLocalDir, BrowserType.ToString(), Profile.Id.ToString()
	);

	private IBrowserInstance? browser;
	public IBrowserInstance Browser => browser ??= BrowserType switch {
		BrowserType.Firefox => new Firefox() { Settings = this },
		_ => new Chromium() { Settings = this }
	};
}
public record EmulationOptions(
	bool Canvas = true,
	bool WebGL = true,
	bool Rects = true,
	bool Font = true,
	bool Audio = true,
	bool Geo = true,
	bool Timezone = true,
	bool WebRTC = true,
	bool Navigator = false
);
#endregion

public class Browzio : IInit {
	public static class State {
		public static bool Staging { get; } = true && IoC.Debug && Debugger.IsAttached;
		public static string? Version { get => IoC.GetValue(nameof(Extensions)); set => IoC.SetValue(nameof(Extensions), value, null); }
	}
	public static class Extensions {
		public static string ProdPath => Path.Combine(FilePaths.AppDataDir, "extensions");
		public static string DevPath => OperatingSystem.IsMacOS()
			? "/Users/dev/src/Chameleon-lib/Chameleon.Assets/addons"
			: @"C:\repos\Chameleon-lib\Chameleon.Assets\addons";

		public static string Chromium => FilePaths.EnsureDirectoryExists(ProdPath, "chromium");
		public static string Chromeleon => Path.Combine(State.Staging && Directory.Exists(DevPath) ? DevPath : Chromium, "chromeleon");

		public static string Gecko => Resources.Assert(ProdPath, "gecko");
		public static string Geckoleon => Path.Combine(State.Staging && Directory.Exists(DevPath) ? DevPath : Gecko, "geckoleon.xpi");
	}
	public static class Factory {
		public static BrowserSetting BrowserSettings(BrowserType bt, BrowserProfile profile) => new(bt, profile) {
			Port = Processez.NextFreePort(9613)
		};
		public static BrowserSetting Chrome(BrowserProfile profile) => BrowserSettings(BrowserType.Chrome, profile);
		public static BrowserSetting Brave(BrowserProfile profile) => BrowserSettings(BrowserType.Brave, profile);
		public static BrowserSetting Firefox(BrowserProfile profile) => BrowserSettings(BrowserType.Firefox, profile);

		public static IBrowserDetector CreateDetector() {
			return OperatingSystem.IsWindows()
				? new WindowsBrowserDetector()
				: OperatingSystem.IsMacOS()
				? new MacOSBrowserDetector()
				: throw new NotSupportedException("Unsupported operating system");
		}
	}
	public static class Utilities {
		private static readonly IBrowserDetector detector = Factory.CreateDetector();

		public static List<BrowserInfo> DetectBrowsers() => detector.DetectedBrowsers;
		public static BrowserInfo? GetBrowser(BrowserType type) => detector.GetBrowser(type);
		public static bool IsInstalled(BrowserType type) => detector.IsInstalled(type);
		public static List<BrowserInfo> GetBrowsersByEngine(BrowserEngine engine) => detector.GetBrowsersByEngine(engine);
	}

	public Browzers Browzas { get; } = new();
	public Serverio Loopback { get; } = new();

	public TaskCompletionSource<bool> Initialized { get; } = new();
	public async Task Init() {
		await Loopback.Init();
		await Loopback.Initialized.Task;
		await Resources.CopyFile("addons", "geckoleon.xpi", Extensions.Gecko);
		await Resources.LoadExtension(ExtensionType.chromeleon, Extensions.Chromium);

		_ = Initialized.TrySetResult(true);
	}

	Browzio() { }
	public static Browzio I { get; } = new();
}