using Chameleon.lib.Common.Enums;
using Chameleon.lib.WebBrowser.System;

namespace Chameleon.lib.WebBrowser.Services.Brave;
public class BraveSysBrowserInstance : SysBrowserInstance {
	public override SystemBrowserType BrowserType { get; set; } = SystemBrowserType.Brave;
}
