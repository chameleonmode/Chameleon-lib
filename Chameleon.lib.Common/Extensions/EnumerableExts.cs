namespace Chameleon.lib.Common.Extensions;
public static class EnumerableExts {
	public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
	{
		foreach (var item in source)
			action(item);
	}

	public static void ForEachOrBreak<T>(this IEnumerable<T> source, Func<T, bool> func)
	{
		foreach (var item in source) {
			bool result = func(item);
			if (result) break;
		}
	}
}
