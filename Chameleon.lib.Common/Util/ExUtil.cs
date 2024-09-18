using System.IO;

namespace Chameleon.lib.Common.Util;

public static class ExUtil {
	public static void TryOrCatch(Action action, Action? caught = null)
	{
		//TODO: refactu ??
		try {
			action();
		} catch {
			caught?.Invoke();
			//ignore
		}
	}

	public static void TryCatch(Action action, Action? caught = null)
	{
		try {
			action();
		} catch (Exception ex) {
			caught?.Invoke();
			Console.WriteLine(ex.ToString());
		}
	}

	public static async Task? AsyncTryCatch(Func<Task> action, Action<Exception>? caught = null)
	{
		try {
			await action();
		} catch (Exception ex) {
			caught?.Invoke(ex);
			Console.WriteLine(ex.ToString());
		}
	}

	private static void OnError(object sender, ErrorEventArgs e) =>
			PrintException(e.GetException());

	private static void PrintException(Exception? ex)
	{
		if (ex != null) {
			Console.WriteLine($"Message: {ex.Message}");
			Console.WriteLine("Stacktrace:");
			Console.WriteLine(ex.StackTrace);
			Console.WriteLine();
			PrintException(ex.InnerException);
		}
	}
}
