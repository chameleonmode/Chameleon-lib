using Chameleon.lib.Common.Interfaces.Systemics;
using Chameleon.lib.Common.Models;

namespace Chameleon.lib.Common.Interfaces.WebBrowser;
public interface ISysBrowserInstance : IHaveInitializer, IDisposable {
	public event Action<SystemBrowserLaunchOptions>? OnProcessClosed;
	public event Action<SystemBrowserLaunchOptions>? OnProcessOpenError;
}
