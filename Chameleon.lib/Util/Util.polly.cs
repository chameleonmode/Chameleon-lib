namespace Chameleon.lib.Util;

public static class PolyUtil {
	public static async Task<T> RetryWithPolicyAsync<T>(
		Func<Task<T>> operation, 
		Action<Exception, int>? OnError = null,
		int maxRetries = 3
	) {
		for (var i = 1; i <= maxRetries; i++) {
			try {
				return await operation();
			} catch (Exception ex) when (i < maxRetries) {
				OnError?.Invoke(ex, i);
				await Task.Delay(2500 * i); // Exponential backoff
			}
		}
		return await operation(); // Last try
	}
}
