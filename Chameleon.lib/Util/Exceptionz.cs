using System.Diagnostics;
using Chameleon.lib.Helpers;

namespace Chameleon.lib.Util;

public static class EX {
	public class CatchPolicy<T, TT>(Action<TT>? caught = null) where TT : Exception {
		private readonly Action<TT>? caught = caught;
		public T? Execute(Func<T> operation) {
			try {
				return operation();
			} catch (TT e) {
				caught?.Invoke(e);
				PrintException(e);
				return default;
			}
		}

		public async Task<T?> Execute(Func<Task<T>> operation) {
			try {
				return await operation();
			} catch (TT e) {
				caught?.Invoke(e);
				PrintException(e);
				return default;
			}
		}
	}

	public static T? Catch<T, TT>(Func<T> action, Action<TT>? caught = null) where TT : Exception {
		var policy = new CatchPolicy<T, TT>(caught);
		return policy.Execute(action);
	}
	public static T? Catch<T>(Func<T> action, Action<Exception>? caught = null) {
		var policy = new CatchPolicy<T, Exception>(caught);
		return Catch<T, Exception>(action, caught);
	}
	public static void Try(Action action, Action<Exception>? caught = null) =>
		Catch<bool, Exception>(() => {
			action();
			return true;
		}, caught);

	public static async Task<T?> Catch<T, TT>(Func<Task<T>> action, Action<TT>? caught = null) where TT : Exception {
		var policy = new CatchPolicy<T, TT>(caught);
		return await policy.Execute(action);
	}
	public static async Task<T?> Catch<T>(Func<Task<T>> action, Action<Exception>? caught = null) {
		return await Catch<T, Exception>(action, caught);
	}
	public static async Task Try(Func<Task> action, Action<Exception>? caught = null) =>
		await Catch<bool, Exception>(async () => {
			await action();
			return true;
		}, caught);

	public class RetryPolicy<T, TT>(Func<TT, Task>? caught = null, int sleep = 2500, int retries = 3) where TT : Exception {
		private readonly Func<TT, Task>? caught = caught;
		private int sleep = sleep;
		private int retries = retries;

		public async Task<T?> Execute(Func<Task<T>> operation) {
			try {
				return await operation();
			} catch (TT e) {
				PrintException(e);
				if (caught != null) await caught(e);
				await Task.Delay(sleep);
				sleep *= 2; retries -= 1;
				return retries > 0 ? await Execute(operation) : default;
			}
		}
	}

	public static async Task<T?> Poly<T, TT>(Func<Task<T>> operation, RetryPolicy<T, TT>? policy = null) where TT : Exception {
		policy ??= new RetryPolicy<T, TT>();
		return await policy.Execute(operation);
	}
	public static async Task<T?> Poly<T>(Func<Task<T>> operation, RetryPolicy<T, Exception>? policy = null) {
		return await Poly<T, Exception>(operation, policy);
	}

	private static void PrintException(Exception? e) {
		if (e == null) return;
		Debug.WriteLine($"Message: {e.Message}\nStackTrace:\n{e.StackTrace}");
		PrintException(e.InnerException);
	}
}
