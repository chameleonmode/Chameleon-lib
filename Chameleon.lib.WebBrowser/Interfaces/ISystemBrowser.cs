using System.Collections.Concurrent;

using Chameleon.lib.Common.Enums;
using Chameleon.lib.WebBrowser.Models;

namespace Chameleon.lib.WebBrowser.Interfaces;
public interface ISysBrowserServiceBase {
	ConcurrentDictionary<int, ISysBrowserInstance> Instances { get; }
	Task<ISysBrowserInstance?> Open(SysBrowserOpenOptions options);
}

public interface IBraveSystemBrowserService : ISysBrowserServiceBase {
}

public interface IChromeSystemBrowserService : ISysBrowserServiceBase {
}

public interface IFirefoxSystemBrowserService : ISysBrowserServiceBase {
}

