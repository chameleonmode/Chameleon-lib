using System.Collections.Concurrent;

using Chameleon.lib.Common;
using Chameleon.lib.Common.Enums;
using Chameleon.lib.Common.Interfaces.WebBrowser;
using Chameleon.lib.Common.Models;

namespace Chameleon.lib.WebBrowser.System;
public abstract class SystemBrowserBase
	: ISystemBrowserBase {
	public ConcurrentDictionary<int, ISysBrowserInstance> Instances { get; } = [];

	public static ISystemBrowserBase? Get(SystemBrowserType browserType) => browserType switch {
		SystemBrowserType.Chrome => IoC.GetService<IChromeSystemBrowser>(),
		SystemBrowserType.Brave => IoC.GetService<IBraveSystemBrowser>(),
		SystemBrowserType.Firefox => IoC.GetService<IFirefoxSystemBrowser>(),
		_ => throw new NotImplementedException(),
	};

	public Task<ISysBrowserInstance> Open(SystemBrowserLaunchOptions options) => throw new NotImplementedException();
}
