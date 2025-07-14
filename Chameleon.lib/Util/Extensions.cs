using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Chameleon.lib.Helpers;

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

  static bool ThrowIf(bool self, string? message = null) {
    return self ? throw new InvalidOperationException(message ?? $"{nameof(self)}, is {self}") : self;
  }
  public static bool ThrowIfFalse(this bool self, string? message = null) => ThrowIf(!self, message);
  public static void ThrowFalse(this bool self, string? message = null) => ThrowIf(!self, message);
  public static bool ThrowIfTrue(this bool self, string? message = null) => ThrowIf(self, message);
  public static void ThrowTrue(this bool self, string? message = null) => ThrowIf(self, message);
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

public static class ListsExtensions {
  public static void UpdateMapped<TSource, TDestination>(this IList<TDestination> cur,
    IEnumerable<TSource> collection,
    Func<TSource, TDestination> mapper,
    Func<TDestination, TSource, bool> contains
  ) {
    var itemsToRemove = cur.Where(destItem => !collection.Any(srcItem => contains(destItem, srcItem)));
    for (var i = cur.Count - 1; i >= 0; i--) {
      if (itemsToRemove.Contains(cur[i])) {
        cur.RemoveAt(i);
      }
    }

    var itemsToAdd = collection.Where(i => !cur.Any(x => contains(x, i))).Select(mapper);
    foreach (var item in itemsToAdd) {
      cur.Add(item);
    }
  }

  public static void AddMapped<TSource, TDestination>(this IList<TDestination> cur,
    IEnumerable<TSource> collection,
    Func<TSource, TDestination> mapper
  ) {
    foreach (var item in collection) {
      var destination = mapper(item);
      cur.Add(destination);
    }
  }

  public static async Task AddMapped<TSource, TDestination>(this IList<TDestination> cur,
    IEnumerable<TSource> collection,
    Func<TSource, Task<TDestination>> mapper
  ) {
    foreach (var item in collection) {
      var destination = await mapper(item);
      cur.Add(destination);
    }
  }

  public static void AddIfNot<T>(this IList<T> list, T item, Func<T, bool> predicate) {
    if (!list.Any(predicate)) list.Add(item);
  }
  public static void AddIfNot<T>(this IList<T> list, T item) {
    if (!list.Contains(item)) list.Add(item);
  }

  public static void ThrowIfAny<T>(this IEnumerable<T> list, Func<T, bool> predicate, string? message = null) {
    if (list.Any(predicate)) throw new InvalidOperationException(message ?? $"Invalid argument. {list.First(predicate)}");
  }

  public static void ForEach<T>(this IEnumerable<T> source, Action<T> action) {
    foreach (var item in source) action(item);
  }
  public static async Task ForEach<T>(this IEnumerable<T> source, Func<T, Task> action, CancellationToken cts = default) {
    foreach (var item in source) await action(item).WaitAsync(cts);
  }

  public static async Task TryEach<T>(this IEnumerable<T> source, Func<T, Task> action, CancellationToken cts = default) {
    await source.ForEach(async item => {
      await EX.Try(
        async () => { await action(item); },
        e => { Toaster.Error($"Error processing item, Error: {e.Message}"); }
      ).WaitAsync(cts);
    }, cts);
  }
  public static void TryEach<T>(this IEnumerable<T> source, Action<T> action) {
    source.ForEach(item => {
      EX.Try(() => action(item));
    });
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



public static class Stringz {
	// Extension Methods
	public static bool Is([NotNullWhen(false)] this string? self) =>
		self == null || self == string.Empty || string.IsNullOrEmpty(self) || string.IsNullOrWhiteSpace(self);
	public static string ThrowIfNullOrEmpty(this string? self) {
		ArgumentException.ThrowIfNullOrEmpty(self);
		return self;
	}
	public static bool IsNot([NotNullWhen(true)] this string? self) => !self.Is();
	public static string Strip(this string self, string prefix) =>
		self.StartsWith(prefix) ? self[prefix.Length..] : self;

	public static object? ParseValue(this string? value) {
		// Try to parse the value as a simple type
		if (int.TryParse(value, out var intValue)) return intValue;
		if (bool.TryParse(value, out var boolValue)) return boolValue;
		if (double.TryParse(value, out var doubleValue)) return doubleValue;
		if (DateTime.TryParse(value, out var dateTimeValue)) return dateTimeValue;

		// If parsing fails, return the original string
		return value;
	}

}