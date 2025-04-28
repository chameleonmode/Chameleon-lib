namespace Chameleon.lib.Util;
public static class Extensions {
	public static TResult Let<T, TResult>(this T self, Func<T, TResult> function) => function(self);

  public static bool IsSimpleType(this Type type) {
    return type.IsPrimitive ||
           type == typeof(string) ||
           type == typeof(decimal) ||
           Nullable.GetUnderlyingType(type) != null ||
           type == typeof(DateTime) ||
           type == typeof(DateTimeOffset) ||
           type == typeof(TimeSpan) ||
           type == typeof(Guid);
  }
}
