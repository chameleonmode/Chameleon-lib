namespace Chameleon.lib.Common.Util;
public static class TaskUtil {

	public static async Task<bool> AwaitFor(Func<bool> contition, int count = 5, int milleseconds = 250, Action<Exception> onfailed = null)
	{
		for (var i = 0; i < count; i++) {
			try {
				if (contition.Invoke())
					return true;
			} catch (Exception e) {
				onfailed?.Invoke(e);
			}
			await Task.Delay(milleseconds);
		}

		return false;
	}

	public static async Task AwaitLoop(Action action, int count = 5, int milleseconds = 250)
	{
		for (var i = 0; i < count; i++) {
			action();

			await Task.Delay(milleseconds);
		}
	}

	public static async Task<T?> TryAwaitFor<T>(Func<T?> contition, int count = 5, int milleseconds = 250)
	{
		for (var i = 0; i < count; i++) {
			try {
				return contition.Invoke();
			} catch { await Task.Delay(milleseconds); }
		}

		return default;
	}
}
