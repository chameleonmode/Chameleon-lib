using Chameleon.lib.Common.Constants;
using Chameleon.lib.WebBrowser.Models;
using Microsoft.Playwright;
using Chameleon.lib.Const;
using Chameleon.AIR.Scripts.Models;
using chameleon.assets;

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
	public static async Task<bool> Init() {
		// var path = Path.Combine(FilePaths.Plugins, "version.json");
		// if (!File.Exists(path)) {
		// 	var content = new {
		// 		Version = "0.0.0",
		// 		Date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
		// 	};
		// 	File.WriteAllText(path, JS.Serialize(content));
		// }
		var source = "plugins.playwright";
		var target = FilePaths.AppDataDir;
		var success = await Resources.Mapped(source, target);
		return success;
	}
}