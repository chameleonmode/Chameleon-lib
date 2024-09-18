using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Common.Enums;

namespace Chameleon.lib.Playwright.Models;
public class PlaywriteRunScriptOptions {
	public int Port { get; set; }
	public bool Record { get; set; } = false;
	public SystemBrowserType BrowserType { get; set; } = SystemBrowserType.Chromium;
	public IBundledCSScript? BundledScript { get; set; }
	public IBundledJSScript? BundledJSScript { get; set; }
	public PlaywrightScriptDescription? Description { get; set; }
}
