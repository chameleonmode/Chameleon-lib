
using Chameleon.lib.Common.Interfaces.Services;
using Chameleon.lib.Common.ServiceManagers;

namespace Chameleon.lib.Common.Services;
public class DispatchService : IDispatchService {
	public void InvokeOnUiThread(Action callback)
	{
		var syncContext = SynchronizationContext.Current;
		var isOnUIThread = syncContext != null && syncContext.GetType() != typeof(SynchronizationContext);

		if (isOnUIThread) {
			callback();
		} else {
			syncContext?.Post(_ => callback(), null);
		}
	}

	public T? InvokeOnUiThread<T>(Func<T?> action)
	{
		var syncContext = SynchronizationContext.Current;
		var isOnUIThread = syncContext != null && syncContext.GetType() != typeof(SynchronizationContext);

		if (isOnUIThread) {
			return action();
		} else {
			T? result = default;
			syncContext?.Send(_ => result = action(), null);
			return result;
		}
	}

	public Task InvokeOnUiThreadAsync<T>(Func<T> action, Action<T>? handler = null, Action? @finally = null)
	{
		return Task.Run(() =>
		{
			try {
				if (!TryExecute(action, out var result)) {
					return;
				}

				if (handler != null) {
					InvokeOnUiThread(() =>
					{
						handler(result);
					});
				}
			} finally {
				@finally?.Invoke();
			}
		});
	}
	public Task InvokeOnUiThreadAsync(Action action, Action<bool>? handler = null, Action? @finally = null)
	{
		return Task.Run(() =>
		{
			try {
				var success = TryExecute(action);

				if (handler != null) {
					InvokeOnUiThread(() =>
					{
						handler(success);
					});
				}
			} finally {
				@finally?.Invoke();
			}
		});
	}

	private bool TryExecute<T>(Func<T?> action, out T? result)
	{
		try {
			result = action();
			return true;
		} catch (Exception ex) {
			Toaster.ShowErr(ex.Message);
		}
		result = default;
		return false;
	}

	private bool TryExecute(Action action)
	{
		return TryExecute(() =>
		{
			action();
			return true;
		}, out var result);
	}
}
