namespace Chameleon.lib.Util;
public static class ULists {
  public static void UpdateMapped<TSource, TDestination>(this IList<TDestination> cur,
    IEnumerable<TSource> collection, Func<TSource, TDestination> mapper, Func<TDestination, TSource, bool> contains
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

  public static void AddMapped<TSource, TDestination>(this IList<TDestination> cur, IEnumerable<TSource> collection, Func<TSource, TDestination> mapper) {
    foreach (var item in collection) {
      var destination = mapper(item);
      cur.Add(destination);
    }
  }

  public static async Task AddMappedAsync<TSource, TDestination>(this IList<TDestination> cur, IEnumerable<TSource> collection, Func<TSource, Task<TDestination>> mapper) {
    foreach (var item in collection) {
      var destination = await mapper(item);
      cur.Add(destination);
    }
  }

	public static void AddIfNotExists<T>(this IList<T> list, T item, Func<T, bool> predicate) {
		if (!list.Any(predicate)) {
			list.Add(item);
		}
	}
}
