using Chameleon.lib.Common.Enums;
using Chameleon.lib.WebBrowser.System;

namespace Chameleon.lib.WebBrowser.Services.Chrome;
public class ChromeSysBrowserInstance : SysBrowserInstance {
	public override SystemBrowserType BrowserType { get; set; } = SystemBrowserType.Chrome;
}
