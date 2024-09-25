using Chameleon.lib.Common.Enums;
using Chameleon.lib.WebBrowser.System;

namespace Chameleon.lib.WebBrowser.Services.Firefox;
public class FirefoxSysBrowserInstance : SysBrowserInstance {
	public override SystemBrowserType BrowserType { get; set; } = SystemBrowserType.Firefox;
}
