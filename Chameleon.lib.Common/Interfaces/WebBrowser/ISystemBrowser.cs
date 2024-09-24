using Chameleon.lib.Common.Models;

namespace Chameleon.lib.Common.Interfaces.WebBrowser;
public interface ISystemBrowserBase {
	ConcurrentDictionary<int, ISysBrowserInstance> Instances { get; }
	Task<ISysBrowserInstance> Open(SystemBrowserLaunchOptions options);
}

public interface IChromeSystemBrowser : ISystemBrowserBase {
}

public interface IFirefoxSystemBrowser : ISystemBrowserBase {
}

public interface IBraveSystemBrowser : ISystemBrowserBase {
}
