using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chameleon.AIR.Actors.Models.Reddit;

public enum Scope { Posts, Communities, Comments, Media, } // People
public enum Sort { Relevance, Hot, Top, New, Comments, }
public enum Filter { All, Year, Month, Week, Today, Hour, }

public record Args : IArgs {
  public string Search { get; set; } = string.Empty;
  public Scope Scope { get; set; } = Scope.Posts;
  public Sort Sort { get; set; } = Sort.Relevance;
  public Filter Filter { get; set; } = Filter.All;

  public IEnumerable<Scope> AvailableScopes => Enum.GetValues<Scope>();
  public IEnumerable<Sort> AvailableSorts => Enum.GetValues<Sort>();
  public IEnumerable<Filter> AvailableFilters => Enum.GetValues<Filter>();
  
  public void Set(DictionaryArgs sourceArgs) {
    Search = GetValue(sourceArgs, "Search", string.Empty) ?? string.Empty;
    Scope = GetValue(sourceArgs, "Scope", Scope.Posts);
    Sort = GetValue(sourceArgs, "Sort", Sort.Relevance);
    Filter = GetValue(sourceArgs, "Filter", Filter.All);
  }

  public DictionaryArgs ToDictionary(IEnumerable<Selection> selections, IEnumerable<string> terms) {
    return new DictionaryArgs {
      ["search"] = terms,
      ["scope"] = Scope.ToString(),
      ["sort"] = Sort.ToString(),
      ["filter"] = Filter.ToString(),
      ["artifacters"] = new List<Artifact>() {
        new() {
          ["type"] = "selections",
          ["data"] = selections.Select(x => x.Script.Title.ToLower())
        }
      }
    };
  }

  private static T? GetValue<T>(DictionaryArgs dictionary, string key, T? defaultValue = default) {
    key = key.ToLowerInvariant();
    if (!dictionary.TryGetValue(key, out var value)) return defaultValue;
    else if (value == null) return defaultValue;
    else if (value is T directValue) return directValue;
    else if (value is JsonElement jsonElement) {
      try {
        if (
          key.Equals("Search", StringComparison.OrdinalIgnoreCase) &&
          typeof(T) == typeof(string) &&
          jsonElement.ValueKind == JsonValueKind.Array
          ) {
          Debug.WriteLine($"Detected array for key '{key}'. Attempting to extract first string element.");
          var stringValues = new List<string>();
          var arrayEnumerator = jsonElement.EnumerateArray();
          while (
            arrayEnumerator.MoveNext() &&
            arrayEnumerator.Current.ValueKind == JsonValueKind.String &&
            arrayEnumerator.Current.GetString()?.Trim() is string term) stringValues.Add(term);
          return (T)(object)string.Join(", ", stringValues);
        } 
        
        return jsonElement.Deserialize<T>(options: new () {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });
      } catch (NotSupportedException nsex) {
        Debug.WriteLine($"Type '{typeof(T).Name}' might not be directly supported for deserialization from JsonElement " +
          $"for key '{key}'. Error: {nsex.Message}");
        return typeof(T) == typeof(int) && jsonElement.TryGetInt32(out var intVal) ? (T)(object)intVal : defaultValue;
      } catch (JsonException jsonEx) {
        Debug.WriteLine($"Failed to deserialize JsonElement for key '{key}' to type '{typeof(T).Name}': {jsonEx.Message}");
      } catch (Exception ex) {
        Debug.WriteLine($"Unexpected error deserializing JsonElement for key '{key}' to type '{typeof(T).Name}': {ex.Message}");
      }
    }
    Debug.WriteLine($"Value for key '{key}' has unexpected type '{value.GetType().Name}' and could not be converted to '{typeof(T).Name}'.");
    return defaultValue;
  }
}
