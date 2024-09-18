using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Common.Enums;

namespace Chameleon.lib.Playwright.Models;
public class PlaywriteRunScriptOptions {
	public int Port { get; set; }
	public bool Record { get; set; } = false;
	public SystemBrowserType BrowserType { get; set; } = SystemBrowserType.Chromium;
	public IBundledScript? BundledScript { get; set; }
	public PlaywrightScriptDescription? Description { get; set; }
}
