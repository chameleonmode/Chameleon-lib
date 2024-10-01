using System.Collections.Concurrent;

using Chameleon.lib.WebBrowser.Models;

namespace Chameleon.lib.WebBrowser.Interfaces;
public interface ISysBrowserService {
	TaskCompletionSource<ISysBrowserInstance?>? OpenTaskCompletionSource { get; }
	ConcurrentDictionary<SysBrowserOpenOptions, ISysBrowserInstance> Instances { get; }
	Task<ISysBrowserInstance?> Open(SysBrowserOpenOptions options);
}
