using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Common.Constants;
using Chameleon.lib.Common.Models;
using Microsoft.Playwright;

namespace Chameleon.lib.Playwright.Models;
public class RunScriptOptions {
	public int Port { get; set; }
	public bool Record { get; set; } = false;
	public Enums.SystemBrowserType BrowserType { get; set; } = Enums.SystemBrowserType.Chromium;
	public IBundledCSScript? BundledCSScript { get; set; }
	public IBundledJSScript? BundledJSScript { get; set; }
	public PlaywrightScriptDescription? Description { get; set; }
}

public record GetCookiesOptions(SysBrowserOpenOptions Browser, int? Port) {
	public Proxy? Proxy => Browser.Profile.Proxy.Server == null ? null
	 : new() {
		 Server = Browser.Profile.Proxy.Server,
		 Username = Browser.Profile.Proxy.UserName,
		 Password = Browser.Profile.Proxy.Password,
	 };

	public string Dir => Path.Combine(Consts.AppDataLocalDir, Browser.BrowserType.ToString(), Browser.Profile.Id.ToString());
}

public record PlaywrightScriptDescription(
	Dictionary<string, string> Parameters, 
	string? Title = null, 
	string? Description = null, 
	string? FilePath = null
);