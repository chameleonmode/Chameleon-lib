using Chameleon.lib.Common.Interfaces.Systemics;

namespace Chameleon.lib.Common.Interfaces.Services;
public interface IDispatcherService : ISingletonDependency {
	void InvokeOnUiThread(Action callback);
	T? InvokeOnUiThread<T>(Func<T?> action);
	Task InvokeOnUiThreadAsync(Action action, Action<bool>? handler = null, Action? @finally = null);
	Task InvokeOnUiThreadAsync<T>(Func<T> action, Action<T>? handler = null, Action? @finally = null);
}
