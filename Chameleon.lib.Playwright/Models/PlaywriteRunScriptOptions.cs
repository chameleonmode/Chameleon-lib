using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Common.Constants;

namespace Chameleon.lib.Playwright.Models;
public class PlaywriteRunScriptOptions {
	public int Port { get; set; }
	public bool Record { get; set; } = false;
	public Enums.SystemBrowserType BrowserType { get; set; } = Enums.SystemBrowserType.Chromium;
	public IBundledCSScript? BundledCSScript { get; set; }
	public IBundledJSScript? BundledJSScript { get; set; }
	public PlaywrightScriptDescription? Description { get; set; }
}
