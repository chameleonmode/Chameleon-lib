using System.Collections.ObjectModel;
using Chameleon.AIR.Scripts.Reddit.Post;
using Chameleon.AIR.Scripts.Reddit.Subreddit;
using Chameleon.lib.AIR.Scripts.Models;

namespace Chameleon.AIR.Actors.Models.Reddit;

public class Actor : IActor {
  public Opts Options { get; set; } = new Opts(
    AI: new AI(
      Decorators: new Decorations(
        System: "You are helpful!",
        Prefix: string.Empty,
        Human: "reddit content creator",
        Audience: "adaptive to the general audience of the task context",
        Background: "surfing reddit",
        Tone: "adaptive to the general tone of context",
        Suffix: string.Empty
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
    new Post(), new Join(),new Vote(),
  };
}