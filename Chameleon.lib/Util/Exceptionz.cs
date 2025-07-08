using System.Diagnostics;

namespace Chameleon.lib.Util;

public static class EX {
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
	public static void Try(Action action, Action<Exception>? caught = null) => Catch(() => { action(); }, caught);
	
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
	public static bool Catch<TT>(Action action, Action<TT>? caught = null) 
		where TT : Exception => Catch(() => { action(); return true; }, caught);
	public static void Try<TT>(Action action, Action<TT>? caught = null) 
		where TT : Exception => Catch(() => { action(); }, caught);

	public static async Task<T?> Catch<T>(Func<Task<T>> action, Func<Exception, Task<T>> caught) {
		try {
			return await action();
		} catch (Exception e) {
			PrintException(e);
			return await caught(e);
		}
	}
	public static async Task<T?> Catch<T>(Func<Task<T>> action, Action<Exception>? caught = null) {
		try {
			return await action();
		} catch (Exception e) {
			caught?.Invoke(e);
			PrintException(e);
			return default;
		}
	}
	public static async Task<bool> Catch(Func<Task> action, Action<Exception>? caught = null) => await Catch(async () => {
		await action();
		return true;
	}, caught);
	public static async Task Try(Func<Task> action, Action<Exception>? caught = null) => await Catch(async () => {
		await action();
	}, caught);
	
	public static async Task<T?> Catch<T, TT>(Func<Task<T>> action, Action<TT>? caught = null)
		where TT : Exception {
		try {
			return await action();
		} catch (TT e) {
			caught?.Invoke(e);
			PrintException(e);
			return default;
		}
	}
	public static async Task<bool> Catch<TT>(Func<Task> action, Action<TT>? caught = null) 
		where TT : Exception => await Catch(async () => {
		await action();
		return true;
	}, caught);
	public static async Task Try<TT>(Func<Task> action, Action<TT>? caught = null) 
		where TT : Exception => await Catch(async () => {
		await action();
	}, caught);

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
	public static async Task<bool> Policy(Func<Task> operation, Action<Exception, int>? caught = null,
		int sleep = 2500, int retries = 3
	) => await Policy(async () => {
		await operation();
		return true;
	}, caught, sleep, retries);
	public static Task TryPolicy(Func<Task> operation, Action<Exception, int>? caught = null,
		int sleep = 2500, int retries = 3
	) => Policy(async () => {
		await operation();
	}, caught, sleep, retries);
	
	public static async Task<T?> Policy<T, TT>(Func<Task<T>> operation, Action<TT, int>? caught = null,
		int sleep = 2500, int retries = 3
	) where TT : Exception {
		try {
			return await operation();
		} catch (TT e) {
			PrintException(e);
			caught?.Invoke(e, retries);
			await Task.Delay(sleep); // Exponential backoff
			return retries > 0 ? await Policy(operation, caught, sleep *= 2, --retries) : default;
		}
	}
	public static async Task<bool> Policy<TT>(Func<Task> operation, Action<TT, int>? caught = null,
		int sleep = 2500, int retries = 3
	) where TT : Exception => await Policy(async () => {
		await operation();
		return true;
	}, caught, sleep, retries);
	public static Task TryPolicy<TT>(Func<Task> operation, Action<TT, int>? caught = null,
		int sleep = 2500, int retries = 3
	) where TT : Exception => Policy(async () => {
		await operation();
	}, caught, sleep, retries);

	private static void PrintException(Exception? e) {
		if (e == null) return;
		Debug.WriteLine($"Message: {e.Message}\nStackTrace:\n{e.StackTrace}");
		PrintException(e.InnerException);
	}
}
