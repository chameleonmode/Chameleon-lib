using System.Diagnostics;

namespace Chameleon.lib.Util;

public static class TaskUtil {

	public static async Task<bool> AwaitFor(Func<bool> contition, int count = 5, int milleseconds = 250, Action<Exception>? onfailed = null) {
		for (var i = 0; i < count; i++) {
			try {
				if (contition.Invoke())
					return true;
			} catch (Exception e) {
				onfailed?.Invoke(e);
				Debug.WriteLine($"AwaitFor failed: {e.Message}");
			}
			await Task.Delay(milleseconds);
		}

		return false;
	}

	public static async Task AwaitLoop(Action action, int count = 5, int milleseconds = 250) {
		for (var i = 0; i < count; i++) {
			action();

			await Task.Delay(milleseconds);
		}
	}

	public static async Task<T?> TryAwaitFor<T>(Func<T?> contition, int count = 5, int milleseconds = 250) {
		for (var i = 0; i < count; i++) {
			try {
				return contition.Invoke();
			} catch (Exception e) {
				Debug.WriteLine($"TryAwaitFor failed: {e.Message}"); await Task.Delay(milleseconds);
			}
		}

		return default;
	}
}
