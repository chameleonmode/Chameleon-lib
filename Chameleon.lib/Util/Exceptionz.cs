using System.Diagnostics;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Util;

public static class Exceptionz {
	public static T? TryCatch<T>(Func<T> action, Action<Exception>? caught = null) {
		try {
			return action();
		} catch (Exception e) {
			caught?.Invoke(e);
			PrintException(e);
			return default;
		}
	}
	public static T? TryCatch<T, TT>(Func<T> action, Action<TT>? caught = null) {
		try {
			return action();
		} catch (Exception e) when (e is TT) {
			caught?.Invoke((TT)(object)e);
			PrintException(e);
			return default;
		}
	}

	public static bool TryCatch(Action action, Action<Exception>? caught = null) {
		return TryCatch(() => { action(); return true; }, caught);
	}

	public static async Task TryCatch(Func<Task> action, Action<Exception>? caught = null) {
		try {
			await action();
		} catch (Exception e) {
			caught?.Invoke(e);
			PrintException(e);
		}
	}

	public static async Task<T?> RetryPolicy<T>(Func<Task<T>> operation,
		Action<Exception, int>? caught = null,
		int sleep = 2500, int retries = 3 
	) {
		try {
			return await operation();
		} catch (Exception e) {
			PrintException(e);
			caught?.Invoke(e, retries);
			await Task.Delay(sleep); // Exponential backoff
			return retries > 0 ? await RetryPolicy(operation, caught, sleep *= 2, --retries) : default;
		}
	}

	private static void PrintException(Exception? e) {
		if (e == null) return;
		Debug.WriteLine($"Message: {e.Message}\nStackTrace:\n{e.StackTrace}");
		PrintException(e.InnerException);
	}
}
