using System.Diagnostics;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Util;

public static class Exceptionz {
	public static T? TryCatch<T>(Func<T> action, Action? caught = null) {
		try {
			return action();
		} catch (Exception ex) {
			caught?.Invoke();
			PrintException(ex);
			return default;
		}
	}

	public static async Task TryCatch(Func<Task> action, Action<Exception>? caught = null, string? what = null) {
		try {
			await action();
		} catch (Exception ex) {
			if (!string.IsNullOrEmpty(what)) {
				Toaster.Error(what, ex.Message);
			}
			caught?.Invoke(ex);
			PrintException(ex);
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

	private static void PrintException(Exception? ex) {
		if (ex == null) return;
		Debug.WriteLine($"Message: {ex.Message}\nStackTrace:\n{ex.StackTrace}");
		PrintException(ex.InnerException);
	}
}
