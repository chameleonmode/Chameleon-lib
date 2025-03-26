namespace Chameleon.lib.Util;
public static class Extensions {
	public static TResult Let<T, TResult>(this T self, Func<T, TResult> function) => function(self);
}
