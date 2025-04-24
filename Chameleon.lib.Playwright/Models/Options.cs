using Chameleon.lib.Common.Constants;
using Chameleon.lib.WebBrowser.Models;
using Microsoft.Playwright;
using Chameleon.lib.Const;
using Chameleon.AIR.Scripts.Models;

namespace Chameleon.lib.Playwright.Models;
public class RunScriptOptions {
	public int Port { get; set; }
	public bool Record { get; set; } = false;
	public Enums.SystemBrowserType BrowserType { get; set; } = Enums.SystemBrowserType.Chromium;
	public IScript? BundledScript { get; set; }
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