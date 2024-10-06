using Chameleon.lib.Common.Interfaces.Systemics;

namespace Chameleon.lib.Common.Interfaces.Services;
public interface IDispatchService : ISingletonDependency {
	void InvokeOnUiThread(Action callback);
	T InvokeOnUiThread<T>(Func<T> action);
}
