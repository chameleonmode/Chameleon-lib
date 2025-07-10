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


}
