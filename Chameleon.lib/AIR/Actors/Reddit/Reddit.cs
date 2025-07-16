using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Chameleon.lib.Abs.Platformatic;

using Chameleon.lib.AIR.Scripts;
using Chameleon.lib.AIR.Scripts.Reddit.Post;
using Chameleon.lib.AIR.Scripts.Reddit.Subreddit;
using Chameleon.lib.AIR.Scripts.Reddit.User;

namespace Chameleon.lib.AIR.Actors.Reddit;
public enum Scope { Posts, Communities, Comments, Media, People }
public enum Sort { Relevance, Hot, Top, New, Comments, Posts }
public enum Filter { All, Year, Month, Week, Today, Hour, }

public record Args(
  Scope Scope = Scope.Posts,
  Sort Sort = Sort.Relevance,
  Filter Filter = Filter.All
) : Actors.Args {
  public IEnumerable<Scope> Scopes => Enum.GetValues<Scope>();
  public IEnumerable<Sort> Sorts => Enum.GetValues<Sort>();
  public IEnumerable<Filter> Filters => Enum.GetValues<Filter>();

  public override Args Set(Artifact sourceArgs) {
    return this with {
      Scope = GetValue(sourceArgs, nameof(Scope), Scope.Posts),
      Sort = GetValue(sourceArgs, nameof(Sort), Sort.Relevance),
      Filter = GetValue(sourceArgs, nameof(Filter), Filter.All)
    };
    // This would require modifying the calling code to handle the new instance
  }

  public override Artifact ToDictionary(IEnumerable<Selection> selections) {
    return new Artifact {
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

  private static T? GetValue<T>(Artifact dictionary, string key, T? defaultValue = default) {
    key = key.ToLowerInvariant();
    if (dictionary.TryGetValue(key, out var value) && value is JsonElement jsonElement) {
      try {
        return jsonElement.Deserialize<T>(options: new() {
          PropertyNameCaseInsensitive = true,
          Converters = { new JsonStringEnumConverter() }
        });
      } catch (Exception ex) {
        Debug.WriteLine($"Failed to deserialize JsonElement for key '{key}' to type '{typeof(T).Name}': {ex.Message}");
        if (ex is NotSupportedException && typeof(T) == typeof(int) && jsonElement.TryGetInt32(out var intVal)) {
          return (T)(object)intVal;
        }
      }
    }
    Debug.WriteLine($"Value for key '{key}' has unexpected type '{value?.GetType().Name}' and could not be converted to '{typeof(T).Name}'.");
    return defaultValue;
  }
}
public class Reddit : IActor {
  public Actors.Args Args { get; set; } = new Args();
  public Opts Options { get; set; } = new Opts(
    AI: new AI(
      Decorators: new Decorations(
        System: "You are a Reddit-native assistant",
        Human: "reddit content creator",
        Audience: "reddit website users",
        Background: "surfing reddit",
        Tone: "adaptive"
      )
    ),
    Args: new() {
      { "Search", string.Empty },
      { "Scope", Scope.Posts.ToString() },
      { "Sort", Sort.Relevance.ToString() },
      { "Filter", Filter.All.ToString() },
    },
    Settings: new Settings(
      Start: new Start(
        Feature: "Reddit",
        Attempts: 9,
        Variations: new Rando(1, 1),
        Iterations: new Rando(1, 1)
      ),
      Timeouts: new Timeouts(30, 15, 60, new Rando(256, 512, null))
    )
  );
  public IEnumerable<IJSScript> Scripts { get; set; } = new ObservableCollection<IJSScript>() {
    new Surf(),
    new Comment(), new Reply(),
    new Post(), new Join(), new Vote(),
    new Follow(),
  };
}