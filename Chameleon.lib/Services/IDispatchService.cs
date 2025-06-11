namespace Chameleon.lib.Services;
public interface IDispatchService  {
	void InvokeOnUiThread(Action callback);
	T InvokeOnUiThread<T>(Func<T> action);
}
