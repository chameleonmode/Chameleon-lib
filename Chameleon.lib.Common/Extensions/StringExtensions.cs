using System.IO;

namespace Chameleon.lib.Common.Extensions;
public static class StringExtensions
{
  public static bool Is(this string? self) => self != null && self != string.Empty && !string.IsNullOrEmpty(self) && !string.IsNullOrWhiteSpace(self);
  public static string? Get(this string self) => self.Is() ? self : null;
  public static string StripPrefix(this string self, string prefix) => self.StartsWith(prefix) ? self[prefix.Length..] : self;

  public static string ToCommaSeparatedString<T>(this IEnumerable<T> self) => string.Join(",", self);

  public static string ToCommaSeparatedString<T>(this List<T> self) => string.Join(",", self);

  public static string[] AddQuotesToEachElement(this IList<string> self) => self.Select(x => $"\"{x}\"").ToArray();

		public static string StripQuotes(this string self) => self.EndsWith('\"') && self.StartsWith('\"') ? self[1..^1] : self;

  public static bool Contains(this string source, string toCheck, StringComparison comp) => source?.IndexOf(toCheck, comp) >= 0;

  public static string EnsureDirectoryExists(this string directoryPath)
  {
    if (!Directory.Exists(directoryPath))
    {
						_ = Directory.CreateDirectory(directoryPath);
    }
    return directoryPath;
  }

  public static bool DeleteDirectory(this string directoryPath)
  {
    try
    {
      Directory.Delete(directoryPath, true);
      return true;
    }
    catch (Exception)
    {
      return false;
    }
  }

  public static bool RecreateDirectory(this string directoryPath)
  {
    if (directoryPath.DeleteDirectory())
    {
						_ = directoryPath.EnsureDirectoryExists();
      return true;
    }
    return false;
  }

  public static string RemoveAfter(this string self, string substr)
  {
    var index = self.LastIndexOf(substr);
				return index == -1 ? self : self.Remove(index);
		}

		public static string RemoveBefore(this string self, string substr)
  {
    var index = self.IndexOf(substr);
				return index == -1 ? self : self.Remove(0, index + 1);
		}

		public static string KiloFormat(this int num) => num >= 100000000
						? (num / 1000000).ToString("#,0M")
						: num >= 10000000
						? (num / 1000000).ToString("0.#") + "M"
						: num >= 100000 ? (num / 1000).ToString("#,0K") : num >= 10000 ? (num / 1000).ToString("0.#") + "K" : num.ToString("#,0");

		public static string CheckFeedForId(this string feedUrl)
  {
    if (!feedUrl.Contains("id="))
    {
      return feedUrl.ToLowerInvariant();
    }

    var startIndex = feedUrl.IndexOf("id=");

    var firstPartFeedUrl = feedUrl[..startIndex].ToLowerInvariant();
    var secondPart = feedUrl[startIndex..];

    return firstPartFeedUrl + secondPart;
  }
}
