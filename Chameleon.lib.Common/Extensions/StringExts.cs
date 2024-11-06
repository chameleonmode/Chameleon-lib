namespace Chameleon.lib.Common.Extensions;
public static class StringExts {
	public static bool Is(this string? self) => self != null && self != string.Empty && !string.IsNullOrEmpty(self) && !string.IsNullOrWhiteSpace(self);
	public static string StripPrefix(this string self, string prefix) => self.StartsWith(prefix) ? self[prefix.Length..] : self;
	public static string ToCommaSeparatedString<T>(this IEnumerable<T> self) => string.Join(",", self);
}
