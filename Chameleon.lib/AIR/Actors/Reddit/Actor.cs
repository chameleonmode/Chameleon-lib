using System.Collections.ObjectModel;
using Chameleon.lib.AIR.Scripts;
using Chameleon.lib.AIR.Scripts.Reddit.Post;
using Chameleon.lib.AIR.Scripts.Reddit.Subreddit;
using Chameleon.lib.AIR.Scripts.Reddit.User;

namespace Chameleon.lib.AIR.Actors.Reddit;

public class Actor : IActor {
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
  public IEnumerable<IScript> Scripts { get; set; } = new ObservableCollection<IJSScript>() {
    new Surf(),
    new Comment(), new Reply(),
    new Post(), new Join(), new Vote(),
    new Follow(),
  };
}