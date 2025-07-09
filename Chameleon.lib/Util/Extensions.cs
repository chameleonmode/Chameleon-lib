using System.ComponentModel;
using System.Text.RegularExpressions;
using Chameleon.lib.Helpers;

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
    foreach (var item in source) action(item);
  }
  public static async Task ForEach<T>(this IEnumerable<T> source, Func<T, Task> action, CancellationToken cts = default)
  {
    foreach (var item in source) await action(item).WaitAsync(cts);
  }

  public static async Task TryEach<T>(this IEnumerable<T> source, Func<T, Task> action, CancellationToken cts = default)
  {
    await source.ForEach(async item =>
    {
      await EX.Try(
        async () => { await action(item); },
        e => { Toaster.Error($"Error processing item, Error: {e.Message}"); }
      ).WaitAsync(cts);
    }, cts);
  }

  public static async Task Empty<T>(this ICollection<T> source, Func<T, Task<bool>> predicate) {
    for (var i = source.Count - 1; i >= 0; i--) {
      var element = source.ElementAt(i);
      if (await predicate(element) == false) continue;
      else source.Remove(element);
    }
  }
  public static void Empty<T>(this ICollection<T> source, Func<T, bool> predicate) {
    for (var i = source.Count - 1; i >= 0; i--) {
      var ele = source.ElementAt(i);
      if (!predicate(ele)) continue;
      else source.Remove(ele);
    }
  }
}

public static class TaskExtensions {
  public static Task RunInBackground<T>(this Task<T> task, CancellationToken cts = default) {
    return Task.Run(() => task, cts);
  }

  public static Task RunInBackground(this Task task, CancellationToken cts = default) {
    return Task.Run(() => task, cts);
  }

  public static async Task<T?> RunInBackgroundWithResult<T>(this Task<T> task, CancellationToken cts = default) {
    return await Task.Run(async () => await task, cts);
  }
}

public static class TagsExtensions {
  public static async Task<string> ToStringAsync(this Task<IEnumerable<string>> fetchTags) {
    var tags = await fetchTags;
    return string.Join(",", tags);
  }

  public static IEnumerable<string> ToTagsList(this string? tags) {
    return string.IsNullOrEmpty(tags) ? [] : tags.Split(",").Select(x => x.Trim());
  }
}

public static class ValidationExtensions {
  public static bool IsValidPhoneNumber(this string? value) {
    if (string.IsNullOrEmpty(value)) return false;

    var pattern = @"^\s*(?:\+?(\d{1,3}))?[-. (]*(\d{3})[-. )]*(\d{3})[-. ]*(\d{4})(?: *x(\d+))?\s*$";
    var regex = new Regex(pattern, RegexOptions.IgnoreCase);

    return regex.IsMatch(value);
  }

  public static bool IsValidWebUrl(this string? value) {

    if (string.IsNullOrEmpty(value)) return false;

    var pattern = @"^((https?|ftp|smtp|http):\/\/)?(www.)?[a-z0-9]+\.[a-z]+(\/[a-zA-Z0-9#]+\/?)*$";

    var regex = new Regex(pattern, RegexOptions.IgnoreCase);

    return regex.IsMatch(value);
  }
}