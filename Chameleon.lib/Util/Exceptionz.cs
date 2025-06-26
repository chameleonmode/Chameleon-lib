using System.Diagnostics;

namespace Chameleon.lib.Util;

public static class Exceptionz {
	public static T? Catch<T>(Func<T> action, Action<Exception>? caught = null) {
		try {
			return action();
		} catch (Exception e) {
			caught?.Invoke(e);
			PrintException(e);
			return default;
		}
	}

	public static bool Catch(Action action, Action<Exception>? caught = null) => Catch(() => { action(); return true; }, caught);
	
	public static T? Catch<T, TT>(Func<T> action, Action<TT>? caught = null)
		where TT : Exception {
		try {
			return action();
		} catch (TT e) {
			caught?.Invoke(e);
			PrintException(e);
			return default;
		}
	}

	public static async Task Catch(Func<Task> action, Action<Exception>? caught = null) {
		try {
			await action();
		} catch (Exception e) {
			caught?.Invoke(e);
			PrintException(e);
		}
	}
	
	public static async Task<T?> Policy<T>(Func<Task<T>> operation, Action<Exception, int>? caught = null,
		int sleep = 2500, int retries = 3
	) {
		try {
			return await operation();
		} catch (Exception e) {
			PrintException(e);
			caught?.Invoke(e, retries);
			await Task.Delay(sleep); // Exponential backoff
			return retries > 0 ? await Policy(operation, caught, sleep *= 2, --retries) : default;
		}
	}

	private static void PrintException(Exception? e) {
		if (e == null) return;
		Debug.WriteLine($"Message: {e.Message}\nStackTrace:\n{e.StackTrace}");
		PrintException(e.InnerException);
	}
}
