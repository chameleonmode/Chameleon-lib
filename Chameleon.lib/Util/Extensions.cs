namespace Chameleon.lib.Util;

public static class Extensions
{
  public static TResult Let<T, TResult>(this T self, Func<T, TResult> function) => function(self);

  public static bool IsSimpleType(this Type type)
  {
    return type.IsPrimitive ||
           type == typeof(string) ||
           type == typeof(decimal) ||
           Nullable.GetUnderlyingType(type) != null ||
           type == typeof(DateTime) ||
           type == typeof(DateTimeOffset) ||
           type == typeof(TimeSpan) ||
           type == typeof(Guid);
  }


  public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
  {
    foreach (var item in source)
      action(item);
  }
  public static async Task ForEach<T>(this IEnumerable<T> source, Func<T, Task> action)
  {
    foreach (var item in source)
      await action(item);
  }

  public static async Task Empty<T>(this IList<T> source, Func<T, Task<bool>> predicate)
  {
    for (var i = source.Count - 1; i >= 0; i--)
    {
      if (await predicate(source.ElementAt(i)))
        source.RemoveAt(i);
    }
  }
}
