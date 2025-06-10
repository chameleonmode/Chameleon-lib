using System.Diagnostics;

namespace Chameleon.lib.Common.Util;

public static class ExUtil {
	public static T? TryCatch<T>(Func<T> action, Action? caught = null)
	{
		try {
			return action();
		} catch (Exception ex) {
			caught?.Invoke();
			PrintException(ex);
		}
		return default;
	}

	public static async Task AsyncTryCatch(Func<Task> action, Action<Exception>? caught = null)
	{
		try {
			await action();
		} catch (Exception ex) {
			caught?.Invoke(ex);
			PrintException(ex);
		}
	}

	private static void PrintException(Exception? ex)
	{
		if (ex != null) {
			Debug.WriteLine($"Message: {ex.Message}");
			Debug.WriteLine("Stacktrace:");
			Debug.WriteLine(ex.StackTrace);
			PrintException(ex.InnerException);
		}
	}
}
