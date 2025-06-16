using System.Diagnostics;

namespace Chameleon.lib.Util;

public static class Exceptionz {
	public static T? TryCatch<T>(Func<T> action, Action? caught = null) {
		try {
			return action();
		} catch (Exception ex) {
			caught?.Invoke();
			PrintException(ex);
		}
		return default;
	}

	public static async Task AsyncTryCatch(Func<Task> action, Action<Exception>? caught = null) {
		try {
			await action();
		} catch (Exception ex) {
			caught?.Invoke(ex);
			PrintException(ex);
		}
	}
	
	public static async Task<T> RetryWithPolicyAsync<T>(
		Func<Task<T>> operation,
		Action<Exception, int>? OnError = null,
		int sleep = 2500,
		int maxRetries = 3
	) {
		for (var i = 1; i <= maxRetries; i++) {
			try {
				return await operation();
			} catch (Exception ex) when (i < maxRetries) {
				PrintException(ex);
				OnError?.Invoke(ex, i);
				await Task.Delay(sleep * i); // Exponential backoff
			}
		}
		return await operation(); // Last try
	}

	private static void PrintException(Exception? ex) {
		if (ex != null) {
			Debug.WriteLine($"Message: {ex.Message}");
			Debug.WriteLine("Stacktrace:");
			Debug.WriteLine(ex.StackTrace);
			PrintException(ex.InnerException);
		}
	}
}
